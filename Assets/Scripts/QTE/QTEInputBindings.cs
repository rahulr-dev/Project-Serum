namespace QTE
{
    public static class QTEInputBindings
    {
        public const string SourceDescription = "Keyboard.current / Gamepad.current (bypasses gameplay InputActions)";

        public static string GetPromptHint(QTEInputKind kind)
        {
            return kind switch
            {
                QTEInputKind.Interact => "Press E or Square",
                QTEInputKind.Jump => "Press Space or Cross",
                QTEInputKind.MoveLeft => "Press A, D-Pad Left, or stick left",
                QTEInputKind.MoveRight => "Press D, D-Pad Right, or stick right",
                QTEInputKind.AnyFaceButton => "Press any face button",
                _ => kind.ToString()
            };
        }

        public static string GetBindingLabel(QTEInputKind kind)
        {
            return kind switch
            {
                QTEInputKind.Interact => "E / Square",
                QTEInputKind.Jump => "Space / Cross",
                QTEInputKind.MoveLeft => "A / D-Pad Left / Stick",
                QTEInputKind.MoveRight => "D / D-Pad Right / Stick",
                QTEInputKind.AnyFaceButton => "Face buttons",
                _ => kind.ToString()
            };
        }
    }
}
