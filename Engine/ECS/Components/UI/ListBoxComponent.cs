using Engine.Core.Runtime;
using Microsoft.Xna.Framework;
using nkast.Aether.Physics2D.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.ECS.Components.UI
{
    public class ListBoxComponent : UIElementComponent
    {
        [Browsable(false)]
        public LayoutComponent? ItemsPanel { get; set; } = null;

        private List<string> _items = new List<string>();
        private int _selectedIndex = -1;

        [Browsable(false)]
        public List<ListBoxItemComponent> CachedItems { get; set; } = new List<ListBoxItemComponent>();

        public List<string> Items
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
                _selectedIndex = value;
                UpdateSelectionState();
            }
        }

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

        // Call this from a system or update loop if you want items to process click states
        public void UpdateList()
        {
            for(int i = 0; i < CachedItems.Count; i++)
            {
                CachedItems[i].UpdateItemInput();
            }
        }

        public void AddItem(string text)
        {
            _items.Add(text);

            if(gameObject.ContextScene == null)
                return;

            // 1. Spawn the row GameObject as a child of the ListBox
            GameObject rowGo = gameObject.ContextScene.Spawn($"ListBoxItem_{CachedItems.Count}", gameObject);

            var rowTransform = rowGo.GetComponent<TransformComponent>();
            if(rowTransform != null)
            {
                rowTransform.SizeX = gameObject.GetComponent<TransformComponent>()?.SizeX ?? 200f;
                rowTransform.SizeY = 24f;
            }

            // 2. Add the ListBoxItemComponent
            var lbi = rowGo.AddComponent<ListBoxItemComponent>();
            lbi.Index = CachedItems.Count;
            lbi.ItemClicked += (clickedItem) => {
                SelectedIndex = clickedItem.Index;
                SelectedIndexChanged?.Invoke(SelectedIndex);
            };

            // 3. Spawn a child GameObject for the text label
            GameObject textGo = gameObject.ContextScene.Spawn("ItemLabel", rowGo);
            var textTransform = textGo.GetComponent<TransformComponent>();
            if(textTransform != null)
            {
                textTransform.LocalPosition = new Vector2(8f, 2f); // Padding offset
            }

            var labelComp = textGo.AddComponent<LabelComponent>();
            labelComp.Text = text;

            // 4. Directly wire the label reference to the item component (No search needed!)
            lbi.Label = labelComp;

            // 5. Cache it
            CachedItems.Add(lbi);
        }

        public void AddItem(string text, object? dataContext = null)
        {
            _items.Add(text);

            if(gameObject.ContextScene == null)
                return;

            GameObject rowGo = gameObject.ContextScene.Spawn($"ListBoxItem_{CachedItems.Count}", gameObject);

            var rowTransform = rowGo.GetComponent<TransformComponent>();
            if(rowTransform != null)
            {
                rowTransform.SizeX = gameObject.GetComponent<TransformComponent>()?.SizeX ?? 200f;
                rowTransform.SizeY = 24f;
            }

            var lbi = rowGo.AddComponent<ListBoxItemComponent>();
            lbi.Index = CachedItems.Count;

            // Assign the payload to the item!
            lbi.DataContext = dataContext;

            lbi.ItemClicked += (clickedItem) => {
                SelectedIndex = clickedItem.Index;
                SelectedIndexChanged?.Invoke(SelectedIndex);
            };

            GameObject textGo = gameObject.ContextScene.Spawn("ItemLabel", rowGo);
            var textTransform = textGo.GetComponent<TransformComponent>();
            if(textTransform != null)
            {
                textTransform.LocalPosition = new Vector2(8f, 2f);
            }

            var labelComp = textGo.AddComponent<LabelComponent>();
            labelComp.Text = text;

            lbi.Label = labelComp;
            CachedItems.Add(lbi);
        }

        public void Populate<T>(IEnumerable<T> dataSource, Func<T, string> displayAction)
        {
            Clear(); // Clear out any old data first

            foreach(var item in dataSource)
            {
                // displayAction runs the lambda to get the text, while 'item' is saved as the DataContext
                AddItem(displayAction(item), item);
            }
        }

        // 3. A helper to easily retrieve the currently selected data
        public T? GetSelectedData<T>() where T : class
        {
            if(_selectedIndex >= 0 && _selectedIndex < CachedItems.Count)
            {
                return CachedItems[_selectedIndex].DataContext as T;
            }
            return null;
        }

        public void RemoveItem(int index)
        {
            if(index < 0 || index >= CachedItems.Count)
                return;

            var itemToRemove = CachedItems[index];
            _items.RemoveAt(index);
            CachedItems.RemoveAt(index);

            // Destroy the row GameObject from the scene hierarchy
            if(gameObject.ContextScene != null && itemToRemove.gameObject != null)
            {
                gameObject.ContextScene.Entities.RemoveEntity(itemToRemove.gameObject);
            }

            // Re-index remaining cached items
            for(int i = 0; i < CachedItems.Count; i++)
            {
                CachedItems[i].Index = i;
            }

            // Adjust selection index if needed
            if(_selectedIndex >= CachedItems.Count)
            {
                SelectedIndex = CachedItems.Count - 1;
            }
            else
            {
                UpdateSelectionState();
            }
        }

        public void Clear()
        {
            foreach(var item in CachedItems)
            {
                if(gameObject.ContextScene != null && item.gameObject != null)
                {
                    gameObject.ContextScene.Entities.RemoveEntity(item.gameObject);
                }
            }
            CachedItems.Clear();
            _items.Clear();
            _selectedIndex = -1;
        }

        public void SetItemText(int index, string newText)
        {
            if(index >= 0 && index < CachedItems.Count)
            {
                _items[index] = newText;
                var label = CachedItems[index].Label;
                if(label != null)
                {
                    label.Text = newText;
                }
            }
        }

        private void UpdateSelectionState()
        {
            for(int i = 0; i < CachedItems.Count; i++)
            {
                CachedItems[i].IsSelected = (i == SelectedIndex);
            }
        }

        private void RebuildList()
        {
            Clear();
            foreach(var text in _items)
            {
                AddItem(text);
            }
        }
    }
}
