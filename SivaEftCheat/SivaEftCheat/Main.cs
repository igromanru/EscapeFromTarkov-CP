﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.UI;
using JsonType;
using SivaEftCheat.Data;
using SivaEftCheat.Options;
using SivaEftCheat.Utils;
using UnityEngine;

namespace SivaEftCheat
{
    class Main : MonoBehaviour
    {
        public static Player LocalPlayer { get; set; }
        public static Camera Camera { get; set; }
        public static GameWorld GameWorld { get; set; }
        public static int ClosePlayers { get; set; }

        internal static List<GameLootItem> LootItems = new List<GameLootItem>();
        internal static List<GameLootContainer> LootableContainers = new List<GameLootContainer>();
        internal static List<GameCorpse> Corpses = new List<GameCorpse>();
        internal static List<GamePlayer> Players = new List<GamePlayer>();

        private IEnumerator _coroutineUpdateMain;
        private float _nextPlayerCacheTime;
        private static readonly float _cachePlayersInterval = 1f;

        private float _nextListCacheTime;
        private static readonly float _cacheListInterval = 1f;

        private void Start()
        {
            AllocConsoleHandler.Open();

            _coroutineUpdateMain = UpdateMain(10f);
            StartCoroutine(_coroutineUpdateMain);
        }

        private void FixedUpdate()
        {
            if (Time.time >= _nextListCacheTime)
            {
                GetLists();
                _nextListCacheTime = (Time.time + _cacheListInterval);
            }

            if (Time.time >= _nextPlayerCacheTime)
            {
                GetPlayers();
                _nextPlayerCacheTime = (Time.time + _cachePlayersInterval);
            }

            foreach (GamePlayer gamePlayer in Players)
                gamePlayer.RecalculateDynamics();

            foreach (GameLootItem gameLootItem in Main.LootItems)
                gameLootItem.RecalculateDynamics();

            foreach (GameLootContainer gameLootContainer in Main.LootableContainers)
                gameLootContainer.RecalculateDynamics();

            foreach (GameCorpse gameCorpse in Corpses)
                gameCorpse.RecalculateDynamics();

        }

        private void GetPlayers()
        {
            try
            {
                Players.Clear();
                ClosePlayers = 0;
                var enumerator = GameWorld.RegisteredPlayers.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    Player player = enumerator.Current;
                    if (player == null)
                        continue;

                    if (player.IsYourPlayer())
                    {
                        LocalPlayer = player;
                        continue;
                    }

                    if (50f > Vector3.Distance(player.Transform.position, Main.LocalPlayer.Transform.position))
                        ClosePlayers++;

                    Players.Add(new GamePlayer(player));
                }
            }
            catch
            {
            }
        }

        private void GetLists()
        {
            try
            {
                if (!MonoBehaviourSingleton<PreloaderUI>.Instance.IsBackgroundBlackActive && Camera != null)
                {
                    var enumerator = GameWorld.LootList.FindAll(item => item is Corpse || item is LootableContainer || item is LootItem).GetEnumerator();
                    LootItems.Clear();
                    LootableContainers.Clear();
                    Corpses.Clear();

                    while (enumerator.MoveNext())
                    {
                        var current = enumerator.Current;
                        if (current is LootItem lootItem)
                        {
                            if (lootItem.gameObject != null && MiscVisualsOptions.DrawItems)
                            {
                                if (MiscVisualsOptions.DrawQuestItems)
                                    if (lootItem.Item.QuestItem)
                                        LootItems.Add(new GameLootItem(lootItem));

                                if (MiscVisualsOptions.DrawMedtems)
                                    if (GameUtils.IsMedItem(lootItem.TemplateId))
                                        LootItems.Add(new GameLootItem(lootItem));

                                if (MiscVisualsOptions.DrawSpecialItems)
                                    if (GameUtils.IsSpecialLootItem(lootItem.TemplateId))
                                        LootItems.Add(new GameLootItem(lootItem));

                                if (MiscVisualsOptions.DrawCommonItems)
                                    if (lootItem.Item.Template.Rarity == ELootRarity.Common)
                                        LootItems.Add(new GameLootItem(lootItem));

                                if (MiscVisualsOptions.DrawRareItems)
                                    if (lootItem.Item.Template.Rarity == ELootRarity.Rare)
                                        LootItems.Add(new GameLootItem(lootItem));

                                if (MiscVisualsOptions.DrawSuperRareItems)
                                    if (lootItem.Item.Template.Rarity == ELootRarity.Superrare)
                                        LootItems.Add(new GameLootItem(lootItem));
                            }
                        }
                        if (current is LootableContainer lootableContainer)
                        {
                            if (lootableContainer.gameObject != null)
                            {
                                if (lootableContainer.ItemOwner.RootItem.GetAllItems().Any(item => GameUtils.IsSpecialLootItem(item.TemplateId)))
                                {
                                    LootableContainers.Add(new GameLootContainer(lootableContainer));
                                }
                            }
                        }
                        if (current is Corpse corpse)
                        {
                            if (PlayerOptions.DrawCorpses)
                            {
                                if (corpse.gameObject != null)
                                {
                                    Corpses.Add(new GameCorpse(corpse));
                                }
                            }
                        }
                    }

                }
            }
            catch { }
        }

        private IEnumerator UpdateMain(float waitTime)
        {
            while (true)
            {
                yield return new WaitForSeconds(waitTime);

                try
                {
                    if (!MonoBehaviourSingleton<PreloaderUI>.Instance.IsBackgroundBlackActive)
                    {
                        GameWorld = Singleton<GameWorld>.Instance;
                        Camera = Camera.main;
                    }
                }
                catch
                {
                }
            }
        }

    }
}
