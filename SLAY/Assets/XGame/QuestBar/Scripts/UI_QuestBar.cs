using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XGame
{
    public class UpdateShowingQuestEvent
    {
        public Quest showingQuest;

        public UpdateShowingQuestEvent(Quest quest)
        {
            this.showingQuest = quest;
        }
    }

    public class UpdateQuestListEvent
    {
    }

    public class UI_QuestBar : UIView
    {
        //任务类型UI预制体
        [SerializeField] private GameObject UI_QuestTypePrefab;
        
        private Transform contentTransform;

        private Dictionary<QuestTypeEnum, UI_QuestType> questTypeDict;

        private TextMeshProUGUI questName;

        private TextMeshProUGUI questDescription;

        private TextMeshProUGUI questProgress;

        private TextMeshProUGUI questStatus;

        private TextMeshProUGUI questReward;

        //提交任务按钮
        private Button submitButton;

        //任务更新测试按钮
        private Button testButton;

        //退出任务UI按钮
        private Button exitButton;

        //是否追踪任务toggle
        private Toggle trackToggle;

        //正在显示的任务
        private Quest showingQuest;


        public override UILayers Layer
        {
            get { return UILayers.NormalLayer; }
        }

        public override bool IsSingle => true;

        public override void OnInit()
        {
            contentTransform = this.transform.Find("background").Find("ScrollView").Find("Viewport").Find("Content");

            questName = transform.Find("background").Find("Panel").Find<TextMeshProUGUI>("questName");
            questDescription = transform.Find("background").Find("Panel").Find<TextMeshProUGUI>("questDescription");
            questReward = transform.Find("background").Find("Panel").Find<TextMeshProUGUI>("questReward");
            questProgress = transform.Find("background").Find("Panel").Find<TextMeshProUGUI>("questProgress");
            questStatus = transform.Find("background").Find("Panel").Find<TextMeshProUGUI>("questStatus");
            submitButton = transform.Find("background").Find("Panel").Find<Button>("submitButton");
            trackToggle = transform.Find("background").Find("Panel").Find<Toggle>("trackToggle");
            testButton = transform.Find("background").Find<Button>("testButton");
            exitButton = transform.Find("background").Find<Button>("exitButton");

            trackToggle.onValueChanged.AddListener(OnIsTrackToggle);
            submitButton.onClick.AddListener(OnSubmitQuestButton);
            testButton.onClick.AddListener(OnTestClick);
            exitButton.onClick.AddListener(onClickExit);

            this.RegisterEvent<UpdateShowingQuestEvent>(UpdateShowingQuestEvent);
            this.RegisterEvent<UpdateQuestListEvent>(UpdateQuestListEvent);

            questTypeDict = new Dictionary<QuestTypeEnum, UI_QuestType>();
            QuestManager.Instance.loadQuest();
        }

        /// <summary>
        /// 修改目前正在显示的任务事件
        /// </summary>
        /// <param name="showingQuestEvent"></param>
        private void UpdateShowingQuestEvent(UpdateShowingQuestEvent showingQuestEvent)
        {
            ShowingQuest = showingQuestEvent.showingQuest;
        }

        /// <summary>
        /// 修改目前的任务缩略列表
        /// </summary>
        /// <param name="updateQuestListEvent"></param>
        private void UpdateQuestListEvent(UpdateQuestListEvent updateQuestListEvent)
        {
            OnShow(null);
        }

        public override void OnShow(object obj)
        {
            List<QuestGroup> questGroupList = QuestManager.Instance.getQuestGroupList();
            UI_QuestType pre = null;
            for (int i = 0; i < questGroupList.Count; i++)
            {
                QuestGroup questGroup = questGroupList[i];
                UI_QuestType uiQuestType = null;
                if (!questTypeDict.ContainsKey(questGroup.QuestTypeEnum))
                {
                    uiQuestType = Instantiate(UI_QuestTypePrefab, contentTransform).GetComponent<UI_QuestType>();
                    uiQuestType.OnInit();
                    questTypeDict.Add(questGroup.QuestTypeEnum, uiQuestType);
                }
                else
                {
                    uiQuestType = questTypeDict[questGroup.QuestTypeEnum];
                }

                //通过链表将几种类型的任务串联起来
                if (pre is not null)
                {
                    pre.next = uiQuestType;
                }

                pre = uiQuestType;

                uiQuestType.OnShow(questGroup);
                uiQuestType.gameObject.SetActive(true);

                if (showingQuest == null && questGroup.currentQuestList != null &&
                    questGroup.currentQuestList.Count != 0)
                {
                    ShowingQuest = questGroup.currentQuestList[0];
                }
            }

            setQuestDetail();
            questTypeDict.Values.First().setNewBias(CommonConstant.QUEST_CATEGORY_HEIGHT);
        }

        /// <summary>
        /// 设置正在显示的任务时，将直接修改对应的UI
        /// </summary>
        public Quest ShowingQuest
        {
            get => showingQuest;
            set
            {
                showingQuest = value;
                QuestManager.Instance.showingQuest = value;
                setQuestDetail();
            }
        }

        /// <summary>
        /// 更新任务详情部分
        /// </summary>
        public void setQuestDetail()
        {
            //失效提交按钮
            submitButton.gameObject.SetActive(false);
            //任务名称
            questName.text = showingQuest.questName;
            //任务详情
            questDescription.text = showingQuest.questDescription;
            //任务奖励
            questReward.text = showingQuest.GetQuestRewardText();
            //任务进度
            questProgress.text = showingQuest.GetQuestProgressText();
            //任务状态
            questStatus.text = EnumUtils.GetQuestStatusDescription(showingQuest.questStatus);
            //提交按钮的显示
            submitButton.gameObject.SetActive(false);
            if ((QuestStatusEnum)showingQuest.questStatus == QuestStatusEnum.ACHIEVED)
            {
                submitButton.gameObject.SetActive(true);
            }

            //任务是否被追踪
            if (QuestManager.Instance.TrackingQuestId == showingQuest.questId)
            {
                trackToggle.isOn = true;
            }
            else
            {
                trackToggle.isOn = false;
            }
        }

        public override void OnHide()
        {
            foreach (UI_QuestType questType in questTypeDict.Values)
            {
                questType.OnHide();
                questType.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 修改是否追踪当前显示任务
        /// </summary>
        /// <param name="isOn"></param>
        private void OnIsTrackToggle(bool isOn)
        {
            trackToggle.isOn = isOn;
            if (isOn)
            {
                QuestManager.Instance.TrackingQuestId = showingQuest.questId;
            }
            else
            {
                if (QuestManager.Instance.TrackingQuestId == showingQuest.questId)
                {
                    QuestManager.Instance.TrackingQuestId = -1;
                }
            }
        }

        /// <summary>
        /// 点击提交任务按钮
        /// </summary>
        private void OnSubmitQuestButton()
        {
            QuestManager.Instance.submitQuest(showingQuest.questId);
        }

        /// <summary>
        /// 测试方法，仅用于测试任务系统功能
        /// </summary>
        private void OnTestClick()
        {
            QuestManager.Instance.updateQuestCondition(QuestConditionTypeEnum.CAPTURE,
                QuestConditionObjectEnum.WOOD, 1);
        }

        /// <summary>
        /// 退出任务UI
        /// </summary>
        private void onClickExit()
        {
            XGame.MainController.HideUI<UI_QuestBar>();
        }
    }
}