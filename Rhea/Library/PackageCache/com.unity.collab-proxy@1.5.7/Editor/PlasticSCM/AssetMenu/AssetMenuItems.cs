using PlasticGui;
using Unity.PlasticSCM.Editor.AssetsOverlays.Cache;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.AssetMenu
{
    internal class AssetMenuItems
    {
        internal static void Enable(
            IAssetMenuOperations operations,
            IAssetStatusCache assetStatusCache,
            AssetOperations.IAssetSelection assetsSelection)
        {
            mOperations = operations;
            mAssetStatusCache = assetStatusCache;
            mAssetsSelection = assetsSelection;

            AddMenuItems();
        }

        internal static void Disable()
        {
            RemoveMenuItems();
        }

        static void AddMenuItems()
        {
            HandleMenuItem.AddMenuItem(
                GetPlasticMenuItemName(PlasticLocalization.Name.PendingChangesPlasticMenu),
                PENDING_CHANGES_MENU_ITEM_PRIORITY,
                PendingChanges, ValidatePendingChanges);
            HandleMenuItem.AddMenuItem(
                GetPlasticMenuItemName(PlasticLocalization.Name.AddPlasticMenu),
                ADD_MENU_ITEM_PRIORITY,
                Add, ValidateAdd);
            HandleMenuItem.AddMenuItem(
                GetPlasticMenuItemName(PlasticLocalization.Name.CheckoutPlasticMenu),
                CHECKOUT_MENU_ITEM_PRIORITY,
                Checkout, ValidateCheckout);
            HandleMenuItem.AddMenuItem(
                GetPlasticMenuItemName(PlasticLocalization.Name.CheckinPlasticMenu),
                CHECKIN_MENU_ITEM_PRIORITY,
                Checkin, ValidateCheckin);
            HandleMenuItem.AddMenuItem(
                GetPlasticMenuItemName(PlasticLocalization.Name.UndoPlasticMenu),
                UNDO_MENU_ITEM_PRIORITY,
                Undo, ValidateUndo);
            HandleMenuItem.AddMenuItem(
                GetPlasticMenuItemName(PlasticLocalization.Name.DiffPlasticMenu),
                GetPlasticShortcut.ForAssetDiff(),
                DIFF_MENU_ITEM_PRIORITY,
                Diff, ValidateDiff);
            HandleMenuItem.AddMenuItem(
                GetPlasticMenuItemName(PlasticLocalization.Name.HistoryPlasticMenu),
                GetPlasticShortcut.ForHistory(),
                HISTORY_MENU_ITEM_PRIORITY,
                History, ValidateHistory);

            HandleMenuItem.UpdateAllMenus();
        }

        static void RemoveMenuItems()
        {
            HandleMenuItem.RemoveMenuItem(
                PlasticLocalization.GetString(PlasticLocalization.Name.PrefixPlasticMenu));

            HandleMenuItem.UpdateAllMenus();
        }

        internal static void PendingChanges()
        {
            ShowWindow.Plastic();

            mOperations.ShowPendingChanges();
        }

        internal static bool ValidatePendingChanges()
        {
            return true;
        }

        internal static void Add()
        {
            mOperations.Add();
        }

        static bool ValidateAdd()
        {
            return ShouldMenuItemBeEnabled(AssetMenuOperations.Add);
        }

        internal static void Checkout()
        {
            mOperations.Checkout();
        }

        static bool ValidateCheckout()
        {
            return ShouldMenuItemBeEnabled(AssetMenuOperations.Checkout);
        }

        internal static void Checkin()
        {
            mOperations.Checkin();
        }

        static bool ValidateCheckin()
        {
            return ShouldMenuItemBeEnabled(AssetMenuOperations.Checkin);
        }

        internal static void Undo()
        {
            mOperations.Undo();
        }

        static bool ValidateUndo()
        {
            return ShouldMenuItemBeEnabled(AssetMenuOperations.Undo);
        }

        internal static void Diff()
        {
            mOperations.ShowDiff();
        }

        static bool ValidateDiff()
        {
            return ShouldMenuItemBeEnabled(AssetMenuOperations.Diff);
        }

        internal static void History()
        {
            ShowWindow.Plastic();

            mOperations.ShowHistory();
        }

        static bool ValidateHistory()
        {
            return ShouldMenuItemBeEnabled(AssetMenuOperations.History);
        }

        static bool ShouldMenuItemBeEnabled(AssetMenuOperations operation)
        {
            if (mOperations == null)
                return false;

            SelectedAssetGroupInfo selectedGroupInfo = SelectedAssetGroupInfo.
                BuildFromAssetList(
                    mAssetsSelection.GetSelectedAssets(),
                    mAssetStatusCache);

            AssetMenuOperations operations = AssetMenuUpdater.
                GetAvailableMenuOperations(selectedGroupInfo);

            return operations.HasFlag(operation);
        }

        static string GetPlasticMenuItemName(PlasticLocalization.Name name)
        {
            return string.Format("{0}/{1}",
                PlasticLocalization.GetString(PlasticLocalization.Name.PrefixPlasticMenu),
                PlasticLocalization.GetString(name));
        }

        static IAssetMenuOperations mOperations;
        static IAssetStatusCache mAssetStatusCache;
        static AssetOperations.IAssetSelection mAssetsSelection;

        const int BASE_MENU_ITEM_PRIORITY = 19; // Puts Plastic SCM right below Create menu

        // incrementing the "order" param by 11 causes the menu system to add a separator
        const int PENDING_CHANGES_MENU_ITEM_PRIORITY = BASE_MENU_ITEM_PRIORITY;
        const int ADD_MENU_ITEM_PRIORITY = PENDING_CHANGES_MENU_ITEM_PRIORITY + 11;
        const int CHECKOUT_MENU_ITEM_PRIORITY = PENDING_CHANGES_MENU_ITEM_PRIORITY + 12;
        const int CHECKIN_MENU_ITEM_PRIORITY = PENDING_CHANGES_MENU_ITEM_PRIORITY + 13;
        const int UNDO_MENU_ITEM_PRIORITY = PENDING_CHANGES_MENU_ITEM_PRIORITY + 14;
        const int DIFF_MENU_ITEM_PRIORITY = PENDING_CHANGES_MENU_ITEM_PRIORITY + 25;
        const int HISTORY_MENU_ITEM_PRIORITY = PENDING_CHANGES_MENU_ITEM_PRIORITY + 26;
    }
}
