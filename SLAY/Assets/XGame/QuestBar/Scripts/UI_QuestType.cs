using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XGame
{
    public class UI_QuestType:UIView
    {
        //列表中的下一项任务类型
        public UI_QuestType next;
        
        //是否展开
        private Toggle toggle;
        
        //任务类型名称
        private TextMeshProUGUI typeName;
        
        //该任务类型的任务列表
        private Dictionary<int, UI_QuestItem> questItemTestDict;

        //任务item预制体
        [SerializeField]
        private GameObject questItemPrefab;

        //相对于父物体的y轴偏移值
        public float biasY = 0;
        
        public override UILayers Layer
        {
            get { return UILayers.NormalLayer; }
        }
        public override void OnInit()
        {
            toggle = transform.Find<Toggle>("Toggle");
            typeName = transform.Find<TextMeshProUGUI>("typeName");
            
            toggle.onValueChanged.AddListener(onShowQuestList);
            
            questItemTestDict = new Dictionary<int, UI_QuestItem>();
        }

        public override void OnShow(object obj)
        {
            QuestGroup questGroup = (QuestGroup)obj;
            typeName.text = EnumUtils.GetQuestTypeDescription((byte)questGroup.QuestTypeEnum);
            if (!toggle.isOn)
            {
                return;
            }
            foreach (Quest quest in questGroup.currentQuestList)
            {
                UI_QuestItem uiQuestItem = null;
                if (!questItemTestDict.ContainsKey(quest.questId))
                {
                    GameObject instantiate = Instantiate(questItemPrefab, transform.parent);
                    uiQuestItem = instantiate.GetComponent<UI_QuestItem>();
                    questItemTestDict.Add(quest.questId, uiQuestItem);
                    uiQuestItem.OnInit();
                }
                else
                {
                    uiQuestItem = questItemTestDict[quest.questId];
                }
                uiQuestItem.OnShow(quest);
                uiQuestItem.gameObject.SetActive(true);

            }
        }

        public override void OnHide()
        {
            
        }

        /// <summary>
        /// 显示或隐藏某任务类型的任务列表
        /// </summary>
        /// <param name="isShow"></param>
        public void onShowQuestList(bool isShow)
        {
            toggle.isOn = isShow;
            foreach (UI_QuestItem uiQuestItemTest in questItemTestDict.Values)
            {
                uiQuestItemTest.setUIValue();
                uiQuestItemTest.gameObject.SetActive(isShow);
            }
            setNewBias(biasY);
          
        }

        /// <summary>
        /// 用链表的形式，将多个任务类型串联起来，并且在某个任务类型的任务列表隐藏或显示时，对之后的所有任务类型进行y轴的重新计算
        /// </summary>
        /// <param name="biasY"></param>
        public void setNewBias(float biasY)
        {
            this.biasY = biasY;
            transform.position = new Vector2(transform.position.x, transform.parent.position.y - this.biasY);
            if (!toggle.isOn)
            {
                if (next is null)
                {
                    return;
                }
                next.setNewBias(this.biasY + CommonConstant.QUEST_CATEGORY_HEIGHT);
            }
            else
            {
                float bias = CommonConstant.QUEST_ITEM_HEIGHT;
                foreach (UI_QuestItem uiQuestItemTest in questItemTestDict.Values)
                {
                    uiQuestItemTest.transform.position = new Vector2(transform.position.x, transform.position.y - bias);
                    bias += CommonConstant.QUEST_ITEM_HEIGHT;
                }

                if (next is null)
                {
                    return;
                }
                next.setNewBias(this.biasY + CommonConstant.QUEST_CATEGORY_HEIGHT + questItemTestDict.Count * CommonConstant.QUEST_ITEM_HEIGHT);
            }
        }
    }
}