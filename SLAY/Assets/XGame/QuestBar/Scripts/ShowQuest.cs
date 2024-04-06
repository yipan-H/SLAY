using UnityEngine;
using UnityEngine.UI;

namespace XGame
{
    public class ShowQuest: UIView
    {
        private Button showQuestButton;
        
        public override bool IsSingle => true;
        public override UILayers Layer
        {
            get { return UILayers.NormalLayer; }
        }

        public override void OnInit()
        {
            showQuestButton = transform.Find<Button>("Button");
        }

        public override void OnShow(object obj)
        {
           showQuestButton.onClick.AddListener(onClickShowQuestBar);
        }

        public override void OnHide()
        {
            
        }
        
        /// <summary>
        /// 仅用于测试中显示任务栏
        /// </summary>
        public void onClickShowQuestBar()
        {
            if (!UIManager.Instance.isShowing<UI_QuestBar>())
            {
                Debug.Log("试试看任务列表展示把");
                XGame.MainController.ShowUI<UI_QuestBar>();
            }
        }
    }
}