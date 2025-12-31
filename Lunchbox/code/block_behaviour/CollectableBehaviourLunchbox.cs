using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

#nullable disable

namespace Lunchbox;

class CollectableBehaviorLunchbox : CollectibleBehaviorHeldBag, IHeldBag
{
    public List<ItemSlotBagContent> _slots;

    public CollectableBehaviorLunchbox(CollectibleObject obj) : base(obj)
    {
    }

    /*
     * Copy of CollectibleBehaviorHeldBag::GetOrCreateSlots but ItemSlotBagContent slots are initialized to FoodSlot 
     * because we need to specify a new ItemSlot for this bag to hold and there is no nice way to do it and we can't override the parent.
     */
    public new List<ItemSlotBagContent> GetOrCreateSlots(ItemStack bagstack, InventoryBase parentinv, int bagIndex, IWorldAccessor world)
    {
        var bagContents = new List<ItemSlotBagContent>();

        string bgcolhex = GetSlotBgColor(bagstack);
        var flags = GetStorageFlags(bagstack);
        int quantitySlots = GetQuantitySlots(bagstack);

        ITreeAttribute stackBackPackTree = bagstack.Attributes.GetTreeAttribute("backpack");
        if (stackBackPackTree == null)
        {
            stackBackPackTree = new TreeAttribute();
            ITreeAttribute slotsTree = new TreeAttribute();

            for (int slotIndex = 0; slotIndex < quantitySlots; slotIndex++)
            {
                ItemSlotBagContent slot = new FoodSlot(parentinv, bagIndex, slotIndex, flags); // Change
                slot.HexBackgroundColor = bgcolhex;
                bagContents.Add(slot);
                slotsTree["slot-" + slotIndex] = new ItemstackAttribute(null);
            }

            stackBackPackTree["slots"] = slotsTree;
            bagstack.Attributes["backpack"] = stackBackPackTree;
        }
        else
        {
            ITreeAttribute slotsTree = stackBackPackTree.GetTreeAttribute("slots");

            foreach (var val in slotsTree)
            {
                int slotIndex = val.Key.Split("-")[1].ToInt();
                ItemSlotBagContent slot = new FoodSlot(parentinv, bagIndex, slotIndex, flags); // Change
                slot.HexBackgroundColor = bgcolhex;

                if (val.Value?.GetValue() != null)
                {
                    ItemstackAttribute attr = (ItemstackAttribute)val.Value;
                    slot.Itemstack = attr.value;
                    slot.Itemstack.ResolveBlockOrItem(world);
                }

                while (bagContents.Count <= slotIndex) bagContents.Add(null);
                bagContents[slotIndex] = slot;
            }
        }

        /*
         * Cache the bagContents before we return because otherwise we cannot access the created slots 
         * for the lunchbox implementation without recreating the slots and we want them to match
         */
        _slots = bagContents;

        /*
         * There seems to be no way to register the lunchbox auto-eat functionality on server connection. 
         * The only place that seems doable is here when the inventory slots are created.
         */
        var lunchbox = collObj as ItemLunchBox;
        lunchbox?.ConfigureAutoEat(world, parentinv);

        return bagContents;
    }
}