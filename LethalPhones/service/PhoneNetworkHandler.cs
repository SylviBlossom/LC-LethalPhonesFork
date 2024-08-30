using Dissonance;
using GameNetcodeStuff;
using Scoops.misc;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace Scoops.service
{
    public class PhoneNetworkHandler : NetworkBehaviour
    {
        public static PhoneNetworkHandler Instance { get; private set; }

        private Dictionary<string, ulong> phoneNumberDict;
        private Dictionary<string, PhoneBehavior> phoneObjectDict;
        private Dictionary<string, string> savedPhoneDict;

        public PlayerPhone localPhone;

        public override void OnNetworkSpawn()
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                if (Instance != null)
                {
                    Instance.gameObject.GetComponent<NetworkObject>().Despawn();
                }
            }

            Instance = this;

            phoneNumberDict = new Dictionary<string, ulong>();
            phoneObjectDict = new Dictionary<string, PhoneBehavior>();
            savedPhoneDict = new Dictionary<string, string>();

            base.OnNetworkSpawn();

            LoadNumbers();
        }

        public void CreateNewPhone(ulong phoneId, string preferredNumber = null, string saveId = null)
        {
            CreateNewPhoneNumberServerRpc(phoneId, preferredNumber, saveId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void CreateNewPhoneNumberServerRpc(ulong phoneId, string preferredNumber = null, string saveId = null, ServerRpcParams serverRpcParams = default)
        {
            ulong clientId = serverRpcParams.Receive.SenderClientId;

            // Order of priority for number loading:
            //   preferred phone number >> saved phone number >> random phone number

            int phoneNumber = Random.Range(0, 10000);
            string phoneString = phoneNumber.ToString("D4");

            if (Config.enablePreferredNumbers.Value && preferredNumber != null && ValidatePhoneNumber(preferredNumber))
            {
                phoneString = preferredNumber;
            }
            else if (Config.savePhoneNumbers.Value && saveId != null && savedPhoneDict.ContainsKey(saveId))
            {
                phoneString = savedPhoneDict[saveId];
            }

            while (phoneNumberDict.ContainsKey(phoneString))
            {
                phoneNumber = Random.Range(0, 10000);
                phoneString = phoneNumber.ToString("D4");
            }

            if (saveId != null)
            {
                savedPhoneDict[saveId] = phoneString;
            }

            PhoneBehavior phone = GetNetworkObject(phoneId).GetComponent<PhoneBehavior>();
            Plugin.Log.LogInfo($"New phone for object: " + phoneId);

            phone.GetComponent<NetworkObject>().ChangeOwnership(clientId);
            phoneNumberDict.Add(phoneString, phone.NetworkObjectId);
            phoneObjectDict.Add(phoneString, phone);

            phone.SetNewPhoneNumberClientRpc(phoneString);
        }

        private bool ValidatePhoneNumber(string phoneString)
        {
            return phoneString.Length == 4 && phoneString.All(char.IsDigit);
        }

        public void DeletePlayerPhone(int playerId)
        {
            PlayerControllerB playerController = StartOfRound.Instance.allPlayerScripts[playerId];
            Plugin.Log.LogInfo("Deleting phone for player: " + playerController.name);
            PlayerPhone phone = playerController.transform.Find("PhonePrefab(Clone)").GetComponent<PlayerPhone>();
            string number = phoneNumberDict.FirstOrDefault(x => x.Value == phone.NetworkObjectId).Key;
            phone.GetComponent<NetworkObject>().RemoveOwnership();
            RemoveNumber(number);
        }

        public void DeletePhone(ulong phoneId)
        {
            Plugin.Log.LogInfo("Deleting phone with ID: " + phoneId);
            PhoneBehavior phone = GetNetworkObject(phoneId).GetComponent<PhoneBehavior>();

            string number = phoneNumberDict.FirstOrDefault(x => x.Value == phone.NetworkObjectId).Key;
            phone.GetComponent<NetworkObject>().RemoveOwnership();
            RemoveNumber(number);
        }

        public void RemoveNumber(string number)
        {
            if (number != null)
            {
                Plugin.Log.LogInfo("Removing number: " + number);

                phoneObjectDict.Remove(number);
                phoneNumberDict.Remove(number);
            }
        }

        public void LoadNumbers()
        {
            if (!GameNetworkManager.Instance.isHostingGame || !Config.savePhoneNumbers.Value)
            {
                return;
            }

            var saveKey = $"{PluginInfo.PLUGIN_GUID}_SavedPhones";

            if (ES3.KeyExists(saveKey, GameNetworkManager.Instance.currentSaveFileName))
            {
                savedPhoneDict = ES3.Load<Dictionary<string, string>>(saveKey, GameNetworkManager.Instance.currentSaveFileName);
            }
        }

        public void SaveNumbers()
        {
            if (!GameNetworkManager.Instance.isHostingGame || !Config.savePhoneNumbers.Value)
            {
                return;
            }

            ES3.Save($"{PluginInfo.PLUGIN_GUID}_SavedPhones", savedPhoneDict, GameNetworkManager.Instance.currentSaveFileName);
        }

        [ServerRpc(RequireOwnership = false)]
        public void MakeOutgoingCallServerRpc(string number, ulong senderId, ServerRpcParams serverRpcParams = default)
        {
            string senderPhoneNumber = phoneNumberDict.FirstOrDefault(x => x.Value == senderId).Key;

            if (phoneNumberDict.ContainsKey(number))
            {
                // Successful call
                phoneObjectDict[number].RecieveCallClientRpc(senderId, senderPhoneNumber);
            }
            else
            {
                // No matching number, failed call
                phoneObjectDict[senderPhoneNumber].InvalidCallClientRpc("Invalid #");
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void AcceptIncomingCallServerRpc(string number, ulong accepterId, ServerRpcParams serverRpcParams = default)
        {
            string accepterPhoneNumber = phoneNumberDict.FirstOrDefault(x => x.Value == accepterId).Key;

            phoneObjectDict[number].CallAcceptedClientRpc(accepterId, accepterPhoneNumber);
        }

        [ServerRpc(RequireOwnership = false)]
        public void HangUpCallServerRpc(string number, ulong cancellerId, ServerRpcParams serverRpcParams = default)
        {
            if (phoneNumberDict.ContainsKey(number))
            {
                string cancellerPhoneNumber = phoneNumberDict.FirstOrDefault(x => x.Value == cancellerId).Key;

                phoneObjectDict[number].HangupCallClientRpc(cancellerId, cancellerPhoneNumber);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void LineBusyServerRpc(string number, ServerRpcParams serverRpcParams = default)
        {
            phoneObjectDict[number].InvalidCallClientRpc("Line Busy");
        }
    }
}