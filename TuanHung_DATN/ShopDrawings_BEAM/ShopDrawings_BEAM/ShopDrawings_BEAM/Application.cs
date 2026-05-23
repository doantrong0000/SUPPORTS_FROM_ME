//using Nice3point.Revit.Toolkit.External;
//using ShopDrawings_BEAM.Commands;

//namespace ShopDrawings_BEAM
//{
//    /// <summary>
//    ///     Application entry point
//    /// </summary>
//    [UsedImplicitly]
//    public class Application : ExternalApplication
//    {
//        public override void OnStartup()
//        {
//            CreateRibbon();
//        }

//        private void CreateRibbon()
//        {
//            var panel = Application.CreatePanel("Commands", "ShopDrawings_BEAM");

//            panel.AddPushButton<StartupCommand>("Execute")
//                .SetImage("/ShopDrawings_BEAM;component/Resources/Icons/RibbonIcon16.png")
//                .SetLargeImage("/ShopDrawings_BEAM;component/Resources/Icons/RibbonIcon32.png");
//        }
//    }
//}