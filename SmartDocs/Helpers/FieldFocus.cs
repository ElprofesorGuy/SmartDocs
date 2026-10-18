namespace SmartDocs.Helpers;

/// <summary>Reproduit l'état « focus » des maquettes : bordure bleue autour du champ.</summary>
public static class FieldFocus
{
    public static void Hook(Entry entry, Border border)
    {
        entry.Focused += (_, _) => { border.Stroke = Palette.Primary; border.StrokeThickness = 1.5; };
        entry.Unfocused += (_, _) => { border.Stroke = Palette.Border; border.StrokeThickness = 1; };
    }
}
