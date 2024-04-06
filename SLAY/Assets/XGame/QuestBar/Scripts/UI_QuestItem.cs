using TMPro;

namespace XGame
{
    public class UI_QuestItem : UIView
    {
        private TextMeshProUGUI questName;

        private TextMeshProUGUI questStatus;

        private Quest quest;

        public override UILayers Layer
        {
            get { return UILayers.NormalLayer; }
        }

        public override void OnInit()
        {
            questName = transform.Find<TextMeshProUGUI>("questName");
            questStatus = transform.Find<TextMeshProUGUI>("questStatus");
        }

        public override void OnShow(object obj)
        {
            if (quest == null)
            {
                quest = (Quest)obj;
            }

            setUIValue();
        }

        public override void OnHide()
        {
        }

        public void OnShowQuest()
        {
            EventCenterManager.Send(new UpdateShowingQuestEvent(quest));
        }

        /// <summary>
        /// 设置UI的值
        /// </summary>
        public void setUIValue()
        {
            if (quest == null)
            {
                return;
            }
            //任务名称
            questName.text = quest.questName;
            //任务状态
            questStatus.text = EnumUtils.GetQuestStatusDescription(quest.questStatus);
        }
    }
}