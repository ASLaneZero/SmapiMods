using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace ConfigurableJojaPrices;

// Draws the original JojaCDMenu checkboxes as list
internal static class JojaDevelopmentListMenu
{
    private const int MenuWidth = 760;
    private const int MenuHeight = 560;
    private const int RowHeight = 72;
    private const int Padding = 32;

    public static void Layout(JojaCDMenu menu)
    {
        menu.xPositionOnScreen = (Game1.uiViewport.Width - MenuWidth) / 2;
        menu.yPositionOnScreen = (Game1.uiViewport.Height - MenuHeight) / 2;

        if (menu.upperRightCloseButton is not null)
        {
            menu.upperRightCloseButton.bounds.X = menu.xPositionOnScreen + MenuWidth - 48;
            menu.upperRightCloseButton.bounds.Y = menu.yPositionOnScreen - 8;
        }

        int visibleRow = 0;
        for (int projectIndex = 0; projectIndex < menu.checkboxes.Count; projectIndex++)
        {
            ClickableComponent checkbox = menu.checkboxes[projectIndex];
            if (checkbox.name == "complete")
            {
                checkbox.bounds = Rectangle.Empty;
                continue;
            }

            checkbox.bounds = new Rectangle(
                menu.xPositionOnScreen + Padding,
                menu.yPositionOnScreen + 104 + (visibleRow * RowHeight),
                MenuWidth - (Padding * 2),
                RowHeight - 8
            );
            visibleRow++;
        }
    }

    public static void Draw(JojaCDMenu menu, SpriteBatch b)
    {
        Layout(menu);
        menu.drawBackground(b);
        IClickableMenu.drawTextureBox(
            b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
            menu.xPositionOnScreen,
            menu.yPositionOnScreen,
            MenuWidth,
            MenuHeight,
            Color.White, 1f, drawShadow: true
        );

        Utility.drawTextWithShadow(
            b, "Joja Community Development", Game1.dialogueFont,
            new Vector2(menu.xPositionOnScreen + Padding, menu.yPositionOnScreen + 28),
            Game1.textColor
        );
        Utility.drawTextWithShadow(
            b, "Select a development to fund:", Game1.smallFont,
            new Vector2(menu.xPositionOnScreen + Padding, menu.yPositionOnScreen + 76),
            Game1.textColor
        );

        int visibleProjects = 0;
        for (int projectIndex = 0; projectIndex < menu.checkboxes.Count; projectIndex++)
        {
            ClickableComponent checkbox = menu.checkboxes[projectIndex];
            if (checkbox.name == "complete")
                continue;

            visibleProjects++;
            (string name, int price) = GetProject(projectIndex);
            bool hovered = checkbox.containsPoint(Game1.getMouseX(), Game1.getMouseY());

            if (hovered)
            {
                IClickableMenu.drawTextureBox(
                    b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                    checkbox.bounds.X, checkbox.bounds.Y,
                    checkbox.bounds.Width, checkbox.bounds.Height,
                    Color.Wheat, 0.65f, drawShadow: false
                );
            }

            Color textColor = Game1.player.Money >= price ? Game1.textColor : Color.Firebrick;
            Utility.drawTextWithShadow(
                b, $"{name}: {price:N0}g", Game1.dialogueFont,
                new Vector2(checkbox.bounds.X + 20, checkbox.bounds.Y + 12),
                textColor
            );
        }

        if (visibleProjects == 0)
        {
            Utility.drawTextWithShadow(
                b, "All community developments are complete.", Game1.dialogueFont,
                new Vector2(menu.xPositionOnScreen + Padding, menu.yPositionOnScreen + 140),
                Game1.textColor
            );
        }

        menu.upperRightCloseButton?.draw(b);
        menu.drawMouse(b);
    }

    private static (string Name, int Price) GetProject(int index) => index switch
    {
        0 => ("Bus", ModEntry.ActiveConfig.Bus),
        1 => ("Minecarts", ModEntry.ActiveConfig.Minecarts),
        2 => ("Bridge", ModEntry.ActiveConfig.QuarryBridge),
        3 => ("Greenhouse", ModEntry.ActiveConfig.Greenhouse),
        4 => ("Panning", ModEntry.ActiveConfig.Panning),
        _ => ("Unknown development", 0)
    };
}