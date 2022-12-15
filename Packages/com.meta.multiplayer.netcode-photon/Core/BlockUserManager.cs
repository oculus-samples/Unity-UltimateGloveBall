// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Oculus.Platform;
using Oculus.Platform.Models;
using UnityEngine;

namespace Meta.Multiplayer.Core
{
    /// <summary>
    /// Manages the blocked users for the current user. It uses the Blocked Users API from Oculus Platform to fetch
    /// the blocked users list and then uses the blocking flow.
    /// https://developer.oculus.com/documentation/unity/ps-blockingsdk/
    /// </summary>
    public class BlockUserManager
    {
        private static BlockUserManager s_instance;

        public static BlockUserManager Instance
        {
            get
            {
                s_instance ??= new BlockUserManager();

                return s_instance;
            }
        }

        private HashSet<ulong> m_blockedUsers = new();

        private BlockUserManager()
        {
        }

        public async Task Initialize()
        {
            var message = await Users.GetBlockedUsers().Gen();
            Debug.Log("EXTRACTING BLOCKED USER DATA");
            if (message.IsError)
            {
                Debug.Log("Could not get the list of users blocked!");
                Debug.LogError(message.GetError());
                return;
            }

            while (message != null)
            {
                var blockedUsers = message.GetBlockedUserList();
                foreach (var user in blockedUsers)
                {
                    Debug.Log("Blocked User: " + user.Id);
                    _ = m_blockedUsers.Add(user.Id);
                }

                message = blockedUsers.HasNextPage ?
                    await Users.GetNextBlockedUserListPage(blockedUsers).Gen() :
                    null;
            }
        }

        public bool IsUserBlocked(ulong userId)
        {
            return m_blockedUsers.Contains(userId);
        }

        public async void BlockUser(ulong userId, Action<ulong> onUserBlockedSuccessful = null)
        {
            if (m_blockedUsers.Contains(userId))
            {
                Debug.LogError($"{userId} is already blocked");
                return;
            }

            var message = await Users.LaunchBlockFlow(userId).Gen();
            if (message.IsError)
            {
                Debug.Log("Error when trying to block the user");
                Debug.LogError(message.Data);
            }
            else
            {
                Debug.Log("Got result: DidBlock = " + message.Data.DidBlock + " DidCancel = " + message.Data.DidCancel);
                if (message.Data.DidBlock)
                {
                    _ = m_blockedUsers.Add(userId);
                    onUserBlockedSuccessful?.Invoke(userId);
                }
            }
        }

        public async void UnblockUser(ulong userId, Action<ulong> onUserUnblockedSuccessful = null)
        {
            if (!m_blockedUsers.Contains(userId))
            {
                Debug.LogError($"{userId} is already unblocked");
                return;
            }

            var message = await Users.LaunchUnblockFlow(userId).Gen();
            if (message.IsError)
            {
                Debug.Log("Error when trying to block the user");
                Debug.LogError(message.Data);
            }
            else
            {
                Debug.Log("Got result: DidUnblock = " + message.Data.DidUnblock + " DidCancel = " + message.Data.DidCancel);
                if (message.Data.DidUnblock)
                {
                    _ = m_blockedUsers.Remove(userId);
                    onUserUnblockedSuccessful?.Invoke(userId);
                }
            }
        }
    }
}