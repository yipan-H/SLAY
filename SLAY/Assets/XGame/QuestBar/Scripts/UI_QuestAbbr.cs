using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using XGame;

namespace XGame
{
    public class UpdateTrackingQuestEvent
    {
        public Quest Quest;

        public UpdateTrackingQuestEvent(Quest quest)
        {
            this.Quest = quest;
        }
    }
    public class UI_QuestAbbr : UIView
    {
        //任务名称
        public TextMeshProUGUI questName;

        //任务进度
        public TextMeshProUGUI questProgress;
        
        //缩略面板
        public Transform panel;
        
        //任务缩略按钮
        public Button hideButton;
        
        //正在追踪的任务
        public Quest quest;

        public Image image;

        //任务缩略UI是否隐藏
        public bool isHide;
        
        public override bool IsSingle => true;
        
        public override UILayers Layer
        {
            get { return UILayers.DefaultLayer; }
        }
        
        public override void OnInit()
        {
            questName = transform.Find<TextMeshProUGUI>("Panel/questName");
            questProgress = transform.Find<TextMeshProUGUI>("Panel/questProgress");
            hideButton = transform.Find<Button>("hideButton");
            panel = transform.Find("Panel");
            image = transform.Find<Image>("hideButton/Image");
            isHide = true;
            
            hideButton.onClick.AddListener(hideOrShow);
            this.RegisterEvent<UpdateTrackingQuestEvent>(updateTrackingQuestEvent);
        }

        public override void OnShow(object obj)
        {
            quest = (Quest)obj;
            questName.text = quest.questName;
            if ((QuestStatusEnum)quest.questStatus == QuestStatusEnum.ACTIVE)
            {
                questProgress.text = quest.GetQuestProgressText();
            }
            else
            {
                questProgress.text = EnumUtils.GetQuestStatusDescription(quest.questStatus);
            }
            
        }

        public override void OnHide()
        {
            
        }

        private void updateTrackingQuestEvent(UpdateTrackingQuestEvent trackingQuestEvent)
        {
            OnShow(trackingQuestEvent.Quest);
        }

        private void hideOrShow()
        {
            isHide = !isHide;
            if (isHide)
            {
                panel.gameObject.SetActive(true);
                image.sprite = Resources.Load("Items/NVArrowRight", typeof(Sprite)) as Sprite;
            }
            else
            {
                panel.gameObject.SetActive(false);
                image.sprite = Resources.Load("Items/NVArrowLeft", typeof(Sprite)) as Sprite;
            }
        }
    }
}

