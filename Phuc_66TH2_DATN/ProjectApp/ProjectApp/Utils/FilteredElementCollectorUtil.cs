using Autodesk.Revit.DB;

namespace ProjectApp.Utils;

public static class FilteredElementCollectorUtil
{
    public static List<ViewFamilyType> View3DTypes(Document document) =>
        new FilteredElementCollector(document).OfClass(typeof(ViewFamilyType))
            .Cast<ViewFamilyType>().Where(x => x.ViewFamily == ViewFamily.ThreeDimensional).ToList();

    public static List<Autodesk.Revit.DB.View> GetViews(Document document)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document), "Document cannot be null.");

        return new FilteredElementCollector(document)
            .OfCategory(BuiltInCategory.OST_Views)
            .OfType<Autodesk.Revit.DB.View>()
            .ToList();
    }

    public static List<ViewPlan> GetViewPlans(Document document) => new FilteredElementCollector(document)
        .OfClass(typeof(ViewPlan))
        .Cast<ViewPlan>()
        .ToList();

    public static List<ViewSheet> GetViewSheets(Document document) => new FilteredElementCollector(document)
        .OfClass(typeof(ViewSheet))
        .OfType<ViewSheet>()
        .ToList();

    public static List<ViewSchedule> GetViewSchedules(Document document) => new FilteredElementCollector(document)
        .OfClass(typeof(ViewSchedule))
        .OfType<ViewSchedule>()
        .Where(x => !x.IsTemplate && x.IsValidSchedule())
        .ToList();

    private static bool IsValidSchedule(this ViewSchedule vs)
    {
        if (vs == null) return false;

        // 1. Bỏ qua schedule đang là template
        if (vs.IsTemplate) return false;

#if REVIT_2022_OR_LATER // tuỳ điều kiện build
            // 2. Revision schedule (Revit 2022+ có property này)
            if (vs.IsRevisionSchedule) return false;
#endif

        // 3. Tên hệ thống '<Revision Schedule>' (dành cho version cũ hơn)
        if (vs.Name != null && vs.Name.Trim().StartsWith("<Revision Schedule>"))
            return false;

        // 4. Các loại schedule “đặc biệt” mà người dùng thường không cần xuất
        if (vs.ViewType == ViewType.PanelSchedule ||
            vs.ViewType == ViewType.ColumnSchedule
           )
            return false;

        // 5. Schedule không có dữ liệu (không cột hoặc không hàng)
        var body = vs.GetTableData()?.GetSectionData(SectionType.Body);
        if (body == null || body.NumberOfColumns == 0)
            return false;

        return true; // còn lại coi là hợp lệ
    }
}