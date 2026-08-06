using Engine.Core.Runtime;
using Engine.Core.Utilities;
using Microsoft.Xna.Framework;
using nkast.Aether.Physics2D.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.ECS.Components.UI
{
    public class ListBoxComponent : UIElementComponent
    {
        [Browsable(false)]
        public LayoutComponent? ItemsPanel { get; set; } = null;

        private List<GameObject> _items = new List<GameObject>();
        private int _selectedIndex = -1;

        [Browsable(false)]
        public List<ListBoxItemComponent> CachedItems { get; set; } = new List<ListBoxItemComponent>();

        [Browsable(false)]
        public List<GameObject> Items
        {
            get => _items;
            set
            {
                _items = value;
                RebuildList();
            }
        }

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if(_selectedIndex != value)
                {
                    _selectedIndex = value;
                    UpdateSelectionState();
                }
            }
        }

        [Browsable(false)]
        public ListBoxItemComponent? SelectedItem =>
            (_selectedIndex >= 0 && _selectedIndex < CachedItems.Count) ? CachedItems[_selectedIndex] : null;

        public event Action<int>? SelectedIndexChanged;

        public override void OnEnabled()
        {
            base.OnEnabled();

            // Ensure the ListBox GameObject has a LayoutComponent for stacking items vertically
            if(gameObject.HasComponent<LayoutComponent>())
            {
                ItemsPanel = gameObject.GetComponent<LayoutComponent>();
            }
            else
            {
                gameObject.AddComponent<LayoutComponent>();
                ItemsPanel = gameObject.GetComponent<LayoutComponent>();
            }

            if(ItemsPanel != null)
            {
                ItemsPanel.Layout = LayoutType.Stack;
                ItemsPanel.Direction = LayoutDirection.Vertical;
            }
        }

        public override void OnDisabled()
        {
            base.OnDisabled();
            Clear(); // Automatically wipe old items when the component or scene disables/stops
        }

        public ListBoxItemComponent AddItem(string text)
        {

            Guid newItemGuid;
            GameObject newItem;
            newItem = gameObject.ContextScene.Spawn($"ListItem_{Items.Count + 1}", this.gameObject);
            Log.Info($"ListBoxComponent: Adding item '{newItem.Name}' to the scene.");
            newItemGuid = newItem.Id;
            var itemComp = new ListBoxItemComponent()
            {
                gameObject = newItem,
                
                ParentListBox = this,
                Index = Items.Count
            };

            newItem.AddComponent(itemComp);
            itemComp.Text = text;
            itemComp.Label.TextSize = 12;
            Items.Add(newItem);
            CachedItems.Add(itemComp);

            return itemComp;
        }

        public List<ListBoxItemComponent>? AddItems(List<string> textList)
        {
            List<ListBoxItemComponent> tempList = new List<ListBoxItemComponent>();
            foreach(var item in textList)
            {
               tempList.Add(AddItem(item));
            }

            return tempList;

        }

        public void Clear()
        {
            foreach(var itemComp in CachedItems)
            {
                if(itemComp.gameObject != null)
                {
                    if(gameObject.ContextScene != null)
                    {
                        gameObject.ContextScene.Entities.RemoveEntity(itemComp.gameObject);
                    }
                    else
                    {
                        itemComp.gameObject.Parent?.RemoveChild(itemComp.gameObject);
                    }
                }
            }
            CachedItems.Clear();
            Items.Clear();
            _selectedIndex = -1;
        }

        public void SetItemText(int index, string newText)
        {
            if(index >= 0 && index < CachedItems.Count)
            {
                CachedItems[index].Text = newText;
            }
        }

        private void UpdateSelectionState()
        {
            for(int i = 0; i < CachedItems.Count; i++)
            {
                CachedItems[i].IsSelected = (i == SelectedIndex);
            }
            SelectedIndexChanged?.Invoke(_selectedIndex);
        }

        private void RebuildList()
        {
            // Helper if resetting raw GameObjects collection
            Clear();
        }
    }
}
