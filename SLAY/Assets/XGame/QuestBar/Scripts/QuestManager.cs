using System;
using System.Collections;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using LitJson;
using Unity.VisualScripting;

namespace XGame
{
    [System.Serializable]
    public class QuestCollection
    {
        public List<Quest> questList;
    }

    /// <summary>
    /// 该类仅用于任务栏的任务显示
    /// </summary>
    public class QuestGroup
    {
        /**
         * 任务类型
         */
        public QuestTypeEnum QuestTypeEnum;

        /**
         * 该类型任务是否隐藏
         */
        public bool hide;
        
        /**
         * 当前任务类型下的可显示任务（激活、已达成条件、已完成)
         */
        public List<Quest> currentQuestList;
    }

    public class QuestManager : Singleton<QuestManager>
    {
        private HashSet<int> finishedQuestSet = new HashSet<int>();

        public Dictionary<string, Quest> questDict = new Dictionary<string, Quest>();

        //正在被追踪的任务ID
        private int trackingQuestId = -1;

        public int TrackingQuestId
        {
            get => trackingQuestId;
            set
            {
                if (!questDict.ContainsKey(value.ToString()))
                {
                    //如果目前的追踪任务ID不存在于任务字典中，则将现实中的缩略UI隐藏
                    if (UIManager.Instance.isShowing<UI_QuestAbbr>())
                    {
                        XGame.MainController.HideUI<UI_QuestAbbr>();
                    }
                    return;
                }
                else
                {
                    //将追踪的任务ID持久化，并更新任务缩略UI
                    trackingQuestId = value;
                    GameData data = GameDataManager.Instance.GetGameData();
                    data.trackingQuestId = trackingQuestId;
                    if (UIManager.Instance.isShowing<UI_QuestAbbr>())
                    {
                        EventCenterManager.Send<UpdateTrackingQuestEvent>(new UpdateTrackingQuestEvent(questDict[trackingQuestId.ToString()]));
                    }
                    else
                    {
                        XGame.MainController.ShowUI<UI_QuestAbbr>(questDict[trackingQuestId.ToString()]);
                    }
                    
                }
                
            }
        }

        //任务UI正在显示的任务
        public Quest showingQuest;


        /// <summary>
        /// 读取任务列表，先从保存数据中获取，如获取不到则解析JSON
        /// </summary>
        /// <param name="json"></param>
        public void loadQuest()
        {
           
            if (questDict.Count != 0)
            {
                return;
            }
            GameData data = GameDataManager.Instance.GetGameData();
            if (data.QuestDict == null)
            {
                parseJson();
                GameDataManager.Instance.setQuestDict();
            }
            else
            {
                questDict = GameDataManager.Instance.Data.QuestDict;
            }
            fillFinishedSet();
        }

        /// <summary>
        /// 读取Json并转化为任务字典
        /// </summary>
        /// <param name="json"></param>
        public void parseJson()
        {
            TextAsset textAsset = Resources.Load<TextAsset>("Json/questList");
            QuestCollection questCollection = JsonUtility.FromJson<QuestCollection>(textAsset.text);
            questDict = questCollection.questList.Select(quest =>
            {
                //如果前置任务不为空，则将其分割并转化为List
                if (!string.IsNullOrEmpty(quest.preQuests))
                {
                    quest.preQuestList = quest.preQuests.Split(",").Select(preQuestId => int.Parse(preQuestId))
                        .ToList();
                }

                //如果任务奖励不为空，则将其转化为奖励列表
                if (!string.IsNullOrEmpty(quest.questRewardObjects) && !string.IsNullOrEmpty(quest.questRewardNums))
                {
                    string[] rewardObjectArray = quest.questRewardObjects.Split(",");
                    string[] rewardNumArray = quest.questRewardNums.Split(",");
                    //奖励数量和奖励物品数量不一致时，直接报错
                    if (rewardNumArray.Length != rewardObjectArray.Length)
                    {
                        throw new ArgumentException(quest.questId + " have different reward Number");
                    }

                    List<QuestReward> questRewardList = new List<QuestReward>();
                    for (int i = 0; i < rewardNumArray.Length; i++)
                    {
                        QuestReward questReward = new QuestReward();
                        questReward.rewardObject = byte.Parse(rewardObjectArray[i]);
                        questReward.rewardNum = int.Parse(rewardNumArray[i]);
                        questRewardList.Add(questReward);
                    }

                    quest.questRewardList = questRewardList;
                }

                //如果任务达成条件不为空，则将其转化为达成条件列表
                if (!string.IsNullOrEmpty(quest.questConditionObjects) &&
                    !string.IsNullOrEmpty(quest.questConditionNums) && !string.IsNullOrEmpty(quest.questConditionTypes))
                {
                    string[] conditionObjectArray = quest.questConditionObjects.Split(",");
                    string[] conditionTypeArray = quest.questConditionTypes.Split(",");
                    string[] conditionNumtArray = quest.questConditionNums.Split(",");
                    //达成条件数量、达成条件物品数量、达成条件类型不一致时，直接报错
                    if (conditionObjectArray.Length != conditionTypeArray.Length ||
                        conditionTypeArray.Length != conditionNumtArray.Length)
                    {
                        throw new ArgumentException(quest.questId + " have different condition Number");
                    }
                    List<QuestCondition> questConditionList = new List<QuestCondition>();
                    for (int i = 0; i < conditionObjectArray.Length; i++)
                    {
                        QuestCondition questCondition = new QuestCondition();
                        questCondition.conditionType = byte.Parse(conditionTypeArray[i]);
                        questCondition.conditionNum = int.Parse(conditionNumtArray[i]);
                        questCondition.conditionObject = int.Parse(conditionObjectArray[i]);
                        questCondition.achieved = false;
                        questCondition.currentNum = 0;
                        questConditionList.Add(questCondition);
                    }

                    quest.questConditionList = questConditionList;
                }

                return quest;
            }).ToDictionary(quest => quest.questId.ToString());
        }


        /// <summary>
        /// 刷新单个任务的状态
        /// 当有任务完成时，调用此任务将与之相关的后置任务设置为激活状态
        /// </summary>
        /// <param name="quest"></param>
        public void freshQuestStatus(Quest quest)
        {
            if (quest == null)
            {
                return;
            }

            if (quest.questStatus != (byte) QuestStatusEnum.HIDE)
            {
                return;
            }
            
            if (quest.preQuestList == null || quest.preQuestList.Count == 0)
            {
                activeQuest(quest);
                return;
            }

            bool allExist = quest.preQuestList.All(questId => finishedQuestSet.Contains(questId));
            if (allExist)
            {
                activeQuest(quest);
            }
        }

        /// <summary>
        /// 刷新任务列表的状态
        /// </summary>
        /// <param name="questList"></param>
        private void freshQuestStatus()
        {
            foreach (string questId in questDict.Keys)
            {
                freshQuestStatus(questDict[questId]);
            }
        }

        /// <summary>
        /// 将任务设为已达标
        /// </summary>
        /// <param name="questId"></param>
        public void achieveQuest(Quest quest)
        {
            if (quest == null)
            {
                Debug.LogError(quest.questId + " not found");
                return;
            }

            if (quest.questStatus != (byte)QuestStatusEnum.ACTIVE)
            {
                Debug.LogError(quest.questId + " is not active");
                return;
            }
            
            quest.questStatus = (byte)QuestStatusEnum.ACHIEVED;
        }

        /// <summary>
        /// 将任务设为已激活
        /// </summary>
        /// <param name="quest"></param>
        public void activeQuest(Quest quest)
        {
            if (quest == null)
            {
                Debug.LogError(quest.questId + " not found");
                return;
            }
            if (quest.questStatus != (byte)QuestStatusEnum.HIDE)
            {
                Debug.LogError(quest.questId + " is not hide");
                return;
            }
            quest.questStatus = (byte)QuestStatusEnum.ACTIVE;
        }
        
        /// <summary>
        /// 将任务设为已完成
        /// </summary>
        /// <param name="quest"></param>
        public void finishQuest(Quest quest)
        {
            if (quest == null)
            {
                Debug.LogError(quest.questId + " not found");
                return;
            }
            if (quest.questStatus != (byte)QuestStatusEnum.ACHIEVED)
            {
                Debug.Log(quest.questId + " is not finished");
                return;
            }
            quest.questStatus = (byte)QuestStatusEnum.FINISHED;
            finishedQuestSet.Add(quest.questId);
            freshQuestStatus();
        }

        /// <summary>
        /// 获取激活的任务列表
        /// </summary>
        /// <returns></returns>
        public List<Quest> getActiveQuestList()
        {
            freshQuestStatus();
            return questDict.Values.Where(quest => quest.questStatus == (byte)QuestStatusEnum.ACTIVE)
                .OrderBy(quest => quest.questId).ToList();
        }

        /// <summary>
        /// 获取已完成的任务列表
        /// </summary>
        /// <returns></returns>
        public List<Quest> getFinishedQuestList()
        {
            freshQuestStatus();
            return questDict.Values.Where(quest => quest.questStatus == (byte)QuestStatusEnum.ACHIEVED || quest.questStatus == (byte) QuestStatusEnum.FINISHED)
                .OrderBy(quest => quest.questId).ToList();
        }

        /// <summary>
        /// 获取除隐藏状态以外的所有任务
        /// </summary>
        /// <returns></returns>
        public List<Quest> getQuestListExceptHide()
        {
            freshQuestStatus();
            return questDict.Values.Where(quest => quest.questStatus != (byte)QuestStatusEnum.HIDE)
                .OrderBy(quest => quest.questId).ToList();
        }
        

        /// <summary>
        /// 填充已完成任务ID，仅当启动游戏时调用
        /// </summary>
        private void fillFinishedSet()
        {
            foreach (Quest quest in questDict.Values)
            {
                if (quest.questStatus == (byte)QuestStatusEnum.ACHIEVED)
                {
                    finishedQuestSet.Add(quest.questId);
                }
            }
        }

        /// <summary>
        /// 更新任务进度，当发生【获取、制作、击杀、捕获、拥有】事件时调用
        /// 该方法只能使激活中的任务变为已完成任务
        /// </summary>
        /// <param name="typeEnum">事件类型</param>
        /// <param name="objectEnum">物品类型</param>
        /// <param name="num">物品数量</param>
        public void updateQuestCondition(QuestConditionTypeEnum typeEnum, QuestConditionObjectEnum objectEnum, int num)
        {
            List<Quest> activeQuestList = getActiveQuestList();
            //需要更新UI的任务列表
            HashSet<Quest> needUpdateQuestSet = new HashSet<Quest>();
            //遍历所有任务
            foreach (Quest quest in activeQuestList)
            {
                //获取当前任务的所有达成条件，如果没有达成条件，则直接设置为已完成
                List<QuestCondition> questQuestConditionList = quest.questConditionList;
                if (questQuestConditionList == null)
                {
                    needUpdateQuestSet.Add(quest);
                    achieveQuest(quest);
                    continue;
                }

                //遍历当前任务的所有达成条件，当达成条件类型和物品类型对应时，检查该条件是否达成
                bool achieved = true;
                foreach (QuestCondition questCondition in questQuestConditionList)
                {
                    if (questCondition.conditionType == (byte)typeEnum &&
                        questCondition.conditionObject == (byte)objectEnum)
                    {
                        //达成条件为拥有时，入参num表示目前玩家拥有的数量，而不是更新的数量，因此不适用累加，而是直接替换旧值
                        if (questCondition.conditionType == (byte)QuestConditionTypeEnum.POSSESS)
                        {
                            questCondition.currentNum = num;
                        }
                        else
                        {
                            questCondition.currentNum += num;
                        }
                        needUpdateQuestSet.Add(quest);
                    }

                    if (questCondition.currentNum < questCondition.conditionNum)
                    {
                        achieved = false;
                    }
                }
                
                //所有条件都达成时，该任务变为完成状态
                if (achieved)
                {
                    achieveQuest(quest);
                }
            }
            //遍历所有需要更新的任务UI
            foreach (Quest quest in needUpdateQuestSet)
            {
                if (quest.questId == showingQuest.questId)
                {
                    EventCenterManager.Send<UpdateShowingQuestEvent>(new UpdateShowingQuestEvent(quest));
                }

                if (quest.questId == trackingQuestId)
                {
                    EventCenterManager.Send<UpdateTrackingQuestEvent>(new UpdateTrackingQuestEvent(quest));
                }
            }
            EventCenterManager.Send<UpdateQuestListEvent>(new UpdateQuestListEvent());
        }

        /// <summary>
        /// 提交任务，并获取报酬
        /// </summary>
        /// <param name="questId"></param>
        public void submitQuest(int questId)
        {
            Quest quest = questDict[questId.ToString()];
            if (quest == null || quest.questStatus != (byte)QuestStatusEnum.ACHIEVED)
            {
                return;
            }
            //修改任务状态为已提交
            finishQuest(quest);
            
            //todo 遍历任务达成条件列表（有些道具在任务达成之后，需要从玩家拥有的道具删除， 策划说暂时没有这样的任务，那就暂时先不管）
            
            //遍历任务奖励列表
            List<QuestReward> questQuestRewardList = quest.questRewardList;
            StringBuilder builder = new StringBuilder();
            builder.Append("player received \n");
            foreach (QuestReward questReward in questQuestRewardList)
            {
                builder.Append(questReward.rewardNum + "" + questReward.rewardObject + "\n");
                //todo 这里需要写玩家获取道具的逻辑，等待后续补充
            }

            if (quest.questId == showingQuest.questId)
            {
                EventCenterManager.Send<UpdateShowingQuestEvent>(new UpdateShowingQuestEvent(quest));
            }

            if (quest.questId == trackingQuestId)
            {
                EventCenterManager.Send<UpdateTrackingQuestEvent>(new UpdateTrackingQuestEvent(quest));
            }
            EventCenterManager.Send<UpdateQuestListEvent>(new UpdateQuestListEvent());
            XGame.MainController.ShowUI<UI_QuestDialog>(builder.ToString());

        }
        

        /// <summary>
        /// 分类型获取当前显示的任务列表
        /// </summary>
        /// <returns></returns>
        public List<QuestGroup> getQuestGroupList()
        {
            freshQuestStatus();
            List<QuestGroup> currentQuestList = new List<QuestGroup>();
            foreach (QuestTypeEnum value in Enum.GetValues(typeof(QuestTypeEnum)))
            {
                QuestGroup questGroup = new QuestGroup();
                currentQuestList.Add(questGroup);
                questGroup.QuestTypeEnum = value;
                // 获取特定任务类型的非隐藏任务，并先按照任务类型排序，再根据
                questGroup.currentQuestList = questDict.Values.Where(quest => quest.questStatus != (byte)QuestStatusEnum.HIDE 
                        && quest.questType == (byte)value).ToList();
            }

            return currentQuestList;
        }
    }
}