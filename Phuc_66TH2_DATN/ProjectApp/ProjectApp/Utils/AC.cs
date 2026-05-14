using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace ProjectApp.Utils;

/// <summary>
/// Lớp tĩnh lưu trữ Context hiện tại của Revit (Document, UIApp, Selection...)
/// Giúp truy cập các đối tượng Revit dễ dàng từ bất kỳ đâu.
/// </summary>
public class AC
{
    public static UIDocument UiDoc;
    public static Document Document;
    public static UIApplication UiApplication;
    public static Autodesk.Revit.UI.Selection.Selection Selection;
    public static Autodesk.Revit.DB.View ActiveView;
    private static ExternalEventHandler externalEventHandler;
    private static ExternalEventHandlers externalEventHandlers;
    private static ExternalEvent externalEvent;
    
    /// <summary>
    /// Khởi tạo các biến môi trường từ dữ liệu CommandData
    /// </summary>
    /// <param name="data">Dữ liệu từ Execute method của IExternalCommand</param>
    /// <param name="currentCommand">Tên command hiện tại (để log nếu cần)</param>
    public static void GetInformation(ExternalCommandData data, string currentCommand)
    {
        var uidoc = data.Application.ActiveUIDocument;
        UiDoc = uidoc;
        Document = uidoc.Document;
        UiApplication = uidoc.Application;
        Selection = uidoc.Selection;
        ActiveView = Document.ActiveView;
    }
    
    // Sự kiện ngoài (External Event) để chạy code Revit API từ ngữ cảnh không đồng bộ (như UI Thread)

    public static ExternalEvent ExternalEvent
    {
        get
        {
            if (externalEvent == null)
            {
                externalEvent = ExternalEvent.Create(ExternalEventHandler);
            }
            return externalEvent;
        }
        set => externalEvent = value;
    }

    public static ExternalEventHandler ExternalEventHandler
    {
        get
        {
            if (externalEventHandler == null)
            {
                externalEventHandler = new ExternalEventHandler();
            }
            return externalEventHandler;
        }
        set => externalEventHandler = value;
    }

    public static ExternalEventHandlers ExternalEventHandlers
    {
        get
        {
            if (externalEventHandlers == null)
            {
                externalEventHandlers = new ExternalEventHandlers();
            }
            return externalEventHandlers;
        }
        set => externalEventHandlers = value;
    }
}