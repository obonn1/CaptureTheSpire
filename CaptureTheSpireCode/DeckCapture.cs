using CaptureTheSpire.CaptureTheSpireCode.Tools;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using System.Collections;
using System.Reflection;

namespace CaptureTheSpire.CaptureTheSpireCode;

internal static class DeckCapture
{
    private const float TopBarHeight = 80f;
    private const float CardTopMargin = 80f;
    private const float CardGridYOffset = 100f;
    private const float CardPadding = 40f;
    private const float FooterHeight = 120f;
    private const float BottomPadding = 24f;
    private const float DeckHorizontalOffset = 0f;
    private const float RelicHeight = 68f;
    private const float RelicSectionPadding = 0f;
    private const float RelicSectionHeight = RelicHeight + RelicSectionPadding;

    public static async Task<Image?> CaptureAsync(NCardGrid liveGrid)
    {
        SubViewport? viewport = null;

        try
        {
            var liveDeckScreen = FindDeckScreen(liveGrid);

            if (liveDeckScreen is null)
            {
                ModLogger.Error(
                    $"Could not find a supported screen containing {liveGrid.GetType().Name}.");
                return null;
            }

            var globalUi = FindAncestor<NGlobalUi>(liveDeckScreen);
            if (globalUi is null)
            {
                ModLogger.Error("Could not find GlobalUi.");
                return null;
            }

            if (liveGrid is null)
            {
                ModLogger.Error("Could not find CardGrid.");
                return null;
            }
            var cardCount = GetCardCount(liveGrid);
            var columns = GetColumnCount(liveGrid);
            var cardSize = GetCardSize(liveGrid);

            if (cardCount <= 0 || columns <= 0 || cardSize.Y <= 0)
            {
                ModLogger.Error($"Could not calculate deck layout. Cards: {cardCount}, columns: {columns}, card size: {cardSize}.");
                return null;
            }

            var rowCount = (int)Math.Ceiling(cardCount / (double)columns);
            var cardsHeight = rowCount * cardSize.Y + Math.Max(0, rowCount - 1) * CardPadding;
            var cardGridTop = liveGrid.Position.Y;
            var finalDeckHeight = cardGridTop + RelicSectionHeight + CardGridYOffset + CardTopMargin + cardsHeight + FooterHeight;
            var outputWidth = Math.Max(1, (int)Math.Ceiling(liveDeckScreen.Size.X));
            var outputHeight = Math.Max(1, (int)Math.Ceiling(finalDeckHeight + BottomPadding));

            if (liveDeckScreen.Duplicate() is not Control duplicatedDeckScreen)
            {
                ModLogger.Error("Could not duplicate NDeckViewScreen.");
                return null;
            }

            if (globalUi.TopBar.Duplicate() is not Control duplicatedTopBar)
            {
                ModLogger.Error("Could not duplicate TopBar.");
                return null;
            }

            var gridPath = liveDeckScreen.GetPathTo(liveGrid);

            var duplicatedGrid = duplicatedDeckScreen.GetNodeOrNull<NCardGrid>(gridPath);
            if (duplicatedGrid is null)
            {
                ModLogger.Error("Could not find duplicated CardGrid before preparation.");
                return null;
            }
            PrepareDuplicatedDeckScreen(duplicatedDeckScreen, duplicatedGrid, liveDeckScreen.Size, finalDeckHeight);
            viewport = CreateViewport(new Vector2I(outputWidth, outputHeight));

            var layoutRoot = CreateLayoutRoot(viewport.Size);
            var deckRoot = CreateDeckRoot(liveDeckScreen.Size, finalDeckHeight);
            var topBarRoot = CreateTopBarRoot(outputWidth);
            var relicRoot = CreateRelicRoot(outputWidth, globalUi.RelicInventory.Position);
            var tree = (SceneTree)Engine.GetMainLoop();

            tree.Root.AddChild(viewport);
            viewport.AddChild(layoutRoot);

            layoutRoot.AddChild(deckRoot);

            duplicatedDeckScreen.Position = Vector2.Zero;
            deckRoot.AddChild(duplicatedDeckScreen);

            await WaitForRenderAsync(tree);

            HideGridBorderGradient(duplicatedGrid);
            duplicatedGrid.SetCanScroll(false);
            duplicatedGrid.Call("SetScrollPosition", 0f);
            duplicatedGrid.Call("ClearGrid");

            RemoveDuplicatedCardHolders(duplicatedGrid);
            CopyGridCardState(liveGrid, duplicatedGrid);
            duplicatedGrid.YOffset = liveGrid.YOffset;
            duplicatedGrid.Call("CalculateRowsNeeded");
            duplicatedGrid.Call("InitGrid");
            duplicatedGrid.Call("ReallocateAll");
            duplicatedGrid.Call("UpdateGridPositions");
            duplicatedGrid.YOffset = liveGrid.YOffset;

            await WaitForRenderAsync(tree);
            await WaitForRenderAsync(tree);
            layoutRoot.AddChild(relicRoot);
            DuplicateRelics(globalUi.RelicInventory, relicRoot);

            layoutRoot.AddChild(topBarRoot);
            duplicatedTopBar.Position = Vector2.Zero;
            topBarRoot.AddChild(duplicatedTopBar);

            await WaitForRenderAsync(tree);
            return viewport.GetTexture().GetImage();
        }
        catch (Exception ex)
        {
            ModLogger.Error($"Deck capture failed: {ex}");
            return null;
        }
        finally
        {
            if (viewport is not null && GodotObject.IsInstanceValid(viewport))
                viewport.QueueFree();
        }
    }

    private static Control? FindDeckScreen(NCardGrid grid)
    {
        for (Node? current = grid.GetParent();
             current is not null;
             current = current.GetParent())
        {
            if (current is NDeckViewScreen
                or NCardPileScreen
                or NCardGridSelectionScreen)
            {
                return (Control)current;
            }
        }

        return null;
    }

    private static void PrepareDuplicatedDeckScreen(Control duplicatedDeckScreen, NCardGrid cardGrid, Vector2 liveScreenSize, float height)
    {
        var screenSize = new Vector2(liveScreenSize.X, height);

        duplicatedDeckScreen.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        duplicatedDeckScreen.Position = Vector2.Zero;
        duplicatedDeckScreen.Size = screenSize;
        duplicatedDeckScreen.CustomMinimumSize = screenSize;
        duplicatedDeckScreen.MouseFilter = Control.MouseFilterEnum.Ignore;

        var gridPosition = new Vector2(0f, cardGrid.Position.Y + RelicSectionHeight);
        var gridSize = new Vector2(liveScreenSize.X, height - gridPosition.Y);

        cardGrid.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        cardGrid.Position = gridPosition;
        cardGrid.Size = gridSize;
        cardGrid.CustomMinimumSize = gridSize;
        cardGrid.SetCanScroll(false);

        var scrollContainer = cardGrid.GetNodeOrNull<Control>("%ScrollContainer");

        if (scrollContainer is null)
        {
            ModLogger.Error("Could not find CardGrid/ScrollContainer.");
            return;
        }

        scrollContainer.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        scrollContainer.Position = new Vector2(175f, 0f);
        scrollContainer.Size = new Vector2(liveScreenSize.X - 350f, gridSize.Y);
        scrollContainer.CustomMinimumSize = scrollContainer.Size;
    }

    private static void CopyGridCardState(NCardGrid source, NCardGrid destination)
    {
        CopyListField(source, destination, "_cards");
        CopyListField(source, destination, "_cardsCache");
        CopyListField(source, destination, "_sortedCardsCache");
        CopyField(source, destination, "_pileType");
        CopyField(source, destination, "_cardSize");
    }

    private static void CopyListField(object source, object destination, string fieldName)
    {
        var field = typeof(NCardGrid).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

        if (field?.GetValue(source) is not IEnumerable sourceItems || field.GetValue(destination) is not IList destinationItems)
        {
            return;
        }

        destinationItems.Clear();

        foreach (var item in sourceItems)
            destinationItems.Add(item);
    }

    private static void CopyField(object source, object destination, string fieldName)
    {
        var field = typeof(NCardGrid).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

        if (field is null)
        {
            return;
        }

        field.SetValue(destination, field.GetValue(source));
    }

    private static void DuplicateRelics(Control liveRelicInventory, Control relicRoot)
    {
        foreach (var child in liveRelicInventory.GetChildren())
        {
            if (child is not Control control || !control.Visible)
                continue;

            if (control.Duplicate() is not Control duplicate)
                continue;

            duplicate.Position = control.Position;
            relicRoot.AddChild(duplicate);
        }
    }

    private static int GetCardCount(NCardGrid grid)
    {
        var cardsField = typeof(NCardGrid).GetField("_cards", BindingFlags.Instance | BindingFlags.NonPublic);

        return cardsField?.GetValue(grid) is ICollection cards
            ? cards.Count
            : 0;
    }

    private static int GetColumnCount(NCardGrid grid)
    {
        var columnsProperty = typeof(NCardGrid).GetProperty("Columns", BindingFlags.Instance | BindingFlags.NonPublic);

        return columnsProperty?.GetValue(grid) is int columns
            ? columns
            : 0;
    }

    private static Vector2 GetCardSize(NCardGrid grid)
    {
        var cardSizeField = typeof(NCardGrid).GetField("_cardSize", BindingFlags.Instance | BindingFlags.NonPublic);

        return cardSizeField?.GetValue(grid) is Vector2 cardSize
            ? cardSize
            : Vector2.Zero;
    }

    private static T? FindVisibleNode<T>() where T : CanvasItem
    {
        var tree = (SceneTree)Engine.GetMainLoop();

        return FindVisibleNode<T>(tree.Root);
    }

    private static T? FindVisibleNode<T>(Node node) where T : CanvasItem
    {
        if (node is T matchingNode && matchingNode.IsVisibleInTree())
            return matchingNode;

        foreach (var child in node.GetChildren())
        {
            var result = FindVisibleNode<T>(child);

            if (result is not null)
                return result;
        }

        return null;
    }

    private static T? FindAncestor<T>(Node node) where T : Node
    {
        var current = node.GetParent();

        while (current is not null)
        {
            if (current is T matchingNode)
                return matchingNode;

            current = current.GetParent();
        }

        return null;
    }

    private static SubViewport CreateViewport(Vector2I outputSize) => new()
    {
        Name = "CaptureTheSpireDeckViewport",
        Size = outputSize,
        TransparentBg = true,
        Disable3D = true,
        RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
    };

    private static Control CreateLayoutRoot(Vector2I outputSize)
    {
        var root = new Control
        {
            Name = "DeckCaptureLayoutRoot",
            Position = Vector2.Zero,
            Size = outputSize,
            CustomMinimumSize = outputSize,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };

        root.SetAnchorsPreset(Control.LayoutPreset.TopLeft);

        return root;
    }

    private static Control CreateDeckRoot(Vector2 liveDeckSize, float height)
    {
        var root = new Control
        {
            Name = "DeckRoot",
            Position = new Vector2(DeckHorizontalOffset, 0),
            Size = new Vector2(liveDeckSize.X, height),
            CustomMinimumSize = new Vector2(liveDeckSize.X, height),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };

        root.SetAnchorsPreset(Control.LayoutPreset.TopLeft);

        return root;
    }

    private static Control CreateTopBarRoot(float outputWidth)
    {
        var root = new Control
        {
            Name = "TopBarRoot",
            Position = Vector2.Zero,
            Size = new Vector2(outputWidth, TopBarHeight),
            CustomMinimumSize = new Vector2(outputWidth, TopBarHeight),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ClipContents = true,
        };

        root.SetAnchorsPreset(Control.LayoutPreset.TopLeft);

        return root;
    }

    private static Control CreateRelicRoot(float outputWidth, Vector2 liveRelicPosition)
    {
        var root = new Control
        {
            Name = "RelicRoot",
            Position = liveRelicPosition,
            Size = new Vector2(outputWidth - liveRelicPosition.X, RelicHeight),
            CustomMinimumSize = new Vector2(outputWidth - liveRelicPosition.X, RelicHeight),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ClipContents = true,
        };

        root.SetAnchorsPreset(Control.LayoutPreset.TopLeft);

        return root;
    }

    private static async Task WaitForRenderAsync(SceneTree tree)
    {
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    private static void RemoveDuplicatedCardHolders(NCardGrid grid)
    {
        var scrollContainer = grid.GetNodeOrNull<Control>("%ScrollContainer");

        if (scrollContainer is null)
        {
            ModLogger.Error("Could not find duplicated grid ScrollContainer.");
            return;
        }

        var holders = scrollContainer.GetChildren()
            .OfType<NGridCardHolder>()
            .ToArray();

        foreach (var holder in holders)
            holder.Free();
    }

    private static void HideGridBorderGradient(NCardGrid grid)
    {
        var borderGradient = grid.GetNodeOrNull<CanvasItem>("BorderGradient");

        if (borderGradient is null)
        {
            ModLogger.Warning("Could not find CardGrid/BorderGradient.");
            return;
        }

        borderGradient.Visible = false;
    }
}