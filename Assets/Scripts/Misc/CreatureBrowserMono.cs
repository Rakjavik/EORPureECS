﻿using rak.ecs.Systems;
using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
namespace rak.UI
{
    public enum CreatureBrowserWindow { Creature_Detail_List }
    public class CreatureBrowserMono : MonoBehaviour
    {
        public const string DETAILTEXT =
            "--Creature Name--\n" +
            "{name}\n" +
            "--Creature State--\n" +
            "{state}\n" +
            "--Current Task--\n" +
            "{task}\n" +
            "--Current Action--\n" +
            "{currentAction}\n" +
            "--Current Task Target--\n" +
            "{taskTarget}\n" +
            "Hunger -- {hungerRelative}-{hunger}\n" +
            "Sleep -- {sleepRelative}-{sleep}\n";

        public static Entity SelectedCreature;

        public TMP_Dropdown creatureDropDown;
        public TMP_Text detailText;
        public TMP_Text clockText;
        public TMP_Text[] memoryText;
        private bool initialized = false;
        private CreatureBrowserWindow currentWindow;
        private static Entity selectedCreature;
        private Entity[] creatureMap;
        private float timeSinceLastUpdate = 0;
        private float updateEvery = .5f;
        public Entity BrowserEntity;
        private EntityManager em;

        public void Initialize(CreatureBrowserWindow startingWindow)
        {
            em = Unity.Entities.World.DefaultGameObjectInjectionWorld.EntityManager;
            creatureMap = null;
            if(startingWindow == CreatureBrowserWindow.Creature_Detail_List)
            {
                NativeArray<Entity> creatures = em.CreateEntityQuery(typeof(CreatureAI)).
                    ToEntityArray(Allocator.TempJob);
                InitializeCreatureList(ref creatures);
                creatures.Dispose();
            }
        }
        private void InitializeCreatureList(ref NativeArray<Entity> creatures)
        {
            if (creatures.Length == 0)
                return;
            //if (initialized) Debug.LogWarning("Initialize called on CreatureBrowser when already initialized");
            creatureDropDown.ClearOptions();
            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
            int creatureLength = creatures.Length;
            for (int count = 0; count < creatureLength; count++)
            {
                TMP_Dropdown.OptionData option = new TMP_Dropdown.OptionData(creatures[count].ToString());
                options.Add(option);
            }
            creatureMap = creatures.ToArray();
            creatureDropDown.AddOptions(options);
            currentWindow = CreatureBrowserWindow.Creature_Detail_List;
            OnDropDownChange();
            initialized = true;
        }
        public void ReplaceCurrentWindowWith(CreatureBrowserWindow replaceWith)
        {
            if(currentWindow == replaceWith)
            {
                Debug.LogWarning("Call to replace window with already current - " + replaceWith);
            }
            currentWindow = replaceWith;

        }

        private void Start()
        {
            Initialize(CreatureBrowserWindow.Creature_Detail_List);
            em = World.DefaultGameObjectInjectionWorld.EntityManager;
        }
        private void RefreshMemoryText()
        {
            int maxRows = 25;
            int maxColumns = memoryText.Length;
            if(!selectedCreature.Equals(Entity.Null))
            {
                DynamicBuffer<ShortMemoryBuffer> memBuffer = em.GetBuffer<ShortMemoryBuffer>(selectedCreature);
                int memoryLength = memBuffer.Length;
                int count = 0;
                for (int column = 0; column < maxColumns; column++)
                {
                    StringBuilder columnText = new StringBuilder();
                    for (int row = 0; row < maxRows; row++)
                    {
                        if (count == memoryLength)
                            break;
                        if (!memBuffer[count].memory.Subject.Equals(Entity.Null))
                        {
                            //if (memBuffer[count].memory.GetInvertVerb())
                                //columnText.Append("!");
                            columnText.Append(memBuffer[count].memory.Verb.ToString() + "-" +
                                memBuffer[count].memory.Subject.ToString() + "\n");
                            //+ " " + memBuffers[count].memory.Iterations + "\n");
                        }
                        else
                        {
                            columnText.Append("Empty\n");
                        }
                        if(row+1 == maxRows)
                        {
                            memoryText[column].text = columnText.ToString();
                        }
                        count++;
                    }
                }
            }
        }
        public void RefreshMainText()
        {
            if (selectedCreature.Equals(Entity.Null) || !em.Exists(selectedCreature))
            {
                NativeArray<Entity> creatures = em.CreateEntityQuery(typeof(CreatureAI)).
                    ToEntityArray(Allocator.TempJob);
                InitializeCreatureList(ref creatures);
                creatures.Dispose();
                return;
            }
            //CreatureState state = em.GetComponentData<CreatureState>(selectedCreature);
            CreatureAI ai = em.GetComponentData<CreatureAI>(selectedCreature);
            Target target = em.GetComponentData<Target>(selectedCreature);
            /*NativeArray<Entity> areaArray = em.CreateEntityQuery(typeof(rak.ecs.area.Area)).ToEntityArray(Allocator.TempJob);
            if(areaArray.Length == 0)
            {
                areaArray.Dispose();
                return;
            }
            ecs.area.Area area = em.GetComponentData<ecs.area.Area>(areaArray[0]);
            Sun sun = em.GetComponentData<Sun>(areaArray[0]);
            ecs.ThingComponents.Needs needs = em.GetComponentData<ecs.ThingComponents.Needs>(selectedCreature);
            string text = DETAILTEXT.Replace("{name}",selectedCreature.ToString());
                text = text.Replace("{state}", state.Value.ToString());
                text = text.Replace("{task}", ai.CurrentTask.ToString());
                text = text.Replace("{taskTarget}", target.targetEntity.ToString());
                text = text.Replace("{currentAction}", ai.CurrentAction.ToString());
                text = text.Replace("{hunger}", needs.Hunger.ToString());
                text = text.Replace("{sleep}", needs.Sleep.ToString());
                detailText.text = text;
            int cc = area.NumberOfCreatures;
            int tc = 0;
            clockText.text = "Creatures-" + cc + " Things-" + tc + " Time-" + sun.AreaLocalTime +
                " Elapsed-" + sun.ElapsedHours + "\n";*/
            // TODO this is ghetto //
            RefreshMemoryText();
            //areaArray.Dispose();
        }
        private void NextTarget(bool back)
        {
            NativeArray<Entity> entities = em.CreateEntityQuery(new ComponentType[] { typeof(CreatureAI) }).ToEntityArray(Allocator.TempJob);
            int newIndex = selectedCreature.Index;
            if (back)
                newIndex -= 1;
            else
                newIndex++;
            if (newIndex < 0)
                newIndex = entities.Length - 1;
            else if (newIndex > entities.Length - 1)
                newIndex = 0;
            if (entities.Length > 0)
                selectedCreature = entities[newIndex];
            entities.Dispose();
        }
        public void SetFocusObject(Entity focus)
        {
            selectedCreature = focus;
            SelectedCreature = selectedCreature;
            FollowCamera.SetFollowTarget(SelectedCreature);
        }
        public void Deactivate()
        {
            gameObject.SetActive(true);
            initialized = false;
        }
        public void OnDropDownChange()
        {
            if (creatureMap.Length == 0) return;
            SetFocusObject(creatureMap[FollowCamera.trackTargetIndex]); 
            RefreshMainText();
        }
        private void Update()
        {
            if (!initialized)
            {
                Initialize(CreatureBrowserWindow.Creature_Detail_List);
                return;
            }
            if (Input.GetKeyUp(KeyCode.A))
            {
                NextTarget(true);
            }
            else if (Input.GetKeyUp(KeyCode.D))
            {
                NextTarget(false);
            }

            timeSinceLastUpdate += Time.deltaTime;
            if (timeSinceLastUpdate > updateEvery)
            {
                timeSinceLastUpdate = 0;
                RefreshMainText();
            }
        }
    }
}
