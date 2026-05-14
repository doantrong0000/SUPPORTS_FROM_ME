using Autodesk.Revit.UI;

namespace ProjectApp.Utils;

public class ExternalEventHandler : IExternalEventHandler
{
    protected static ExternalEventHandler instance { get; set; } = null;

    public static ExternalEventHandler Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new ExternalEventHandler();
            }

            return instance;
        }
    }

    protected static ExternalEvent create { get; set; } = null;

    public ExternalEvent Create()
    {
        if (create == null)
        {
            create = ExternalEvent.Create(Instance);
        }

        return create;
    }

    protected static Action Action;

    public void SetAction(Action parameter)
    {
        Action = parameter;
    }

    public async void Run()
    {
        create.Raise();

        while (create.IsPending)
        {
            await System.Threading.Tasks.Task.Delay(10);
        }
    }

    public void Execute(UIApplication app)
    {
        UIDocument uidoc = app.ActiveUIDocument;

        if (uidoc == null)
        {
            TaskDialog.Show("Notification", " no document, nothing to do");
            return;
        }

        Action();
    }

    public string GetName()
    {
        return GetType().Name;
    }
}

public class ExternalEventHandlers : IExternalEventHandler
{
    public List<Action> Actions { get; set; } = new List<Action>();
    public void Execute(UIApplication app)
    {
        Actions.ForEach(x => x());
    }

    public string GetName()
    {
        return "External Events";
    }
}