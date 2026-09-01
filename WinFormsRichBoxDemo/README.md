# WinForms RichTextBox + Custom Button Demo

A small .NET Framework 4.8 WinForms demo showing:

- A `RichTextBox` log with a **white background** and **bold black** log
  entries (`MainForm.AppendLog`).
- A reusable `CustomButton` (flat, rounded corners, hover/press colors) used
  for a red **Root** button plus two sample buttons (**Add Log**, **Clear**).

## Run it (Windows + Visual Studio)

1. Open `WinFormsRichBoxDemo.csproj` in Visual Studio 2019/2022 (or double
   click it — VS will offer to create a matching `.sln`).
2. Make sure the **.NET Framework 4.8 targeting pack** is installed.
3. Press **F5**. Click **Add Log** a few times, click **Root** to see the
   highlighted privileged-action entry, and **Clear** to wipe the log.

## Files

| File | Purpose |
|---|---|
| `CustomButton.cs` | Reusable flat/rounded `Button` subclass with `NormalColor` / `HoverColor` / `PressColor` / `CornerRadius`. |
| `MainForm.cs` / `MainForm.Designer.cs` | Form layout: white `RichTextBox` + the three buttons. |
| `Program.cs` | Standard WinForms entry point. |

## Restyling

- Log text color/weight: edit `AppendLog(...)` in `MainForm.cs` (it sets
  `SelectionColor` + `SelectionFont` per appended line, so you can mix
  colors for e.g. errors vs. info without changing the box's white
  background).
- Button colors/roundness: set `NormalColor`, `HoverColor`, `PressColor`,
  `CornerRadius` on any `CustomButton` instance.
