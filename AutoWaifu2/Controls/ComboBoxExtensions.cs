using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace AutoWaifu2
{
    static class ComboBoxExtensions
    {
        static Dictionary<ComboBox, object> ComboBoxValueMappings = new Dictionary<ComboBox, object>();

        //  Really should move to proper MVVM

        public static void ExtSetMappings<T>(this ComboBox cbx, Dictionary<string, T> valueMaps, T initialValue = default(T))
        {
            if (ComboBoxValueMappings.ContainsKey(cbx))
                ComboBoxValueMappings.Remove(cbx);

            ComboBoxValueMappings.Add(cbx, valueMaps);

            cbx.ItemsSource = valueMaps.Keys.ToList();
        }

        public static void ExtSetValue<T>(this ComboBox cbx, T value)
        {
            var extMap = ComboBoxValueMappings[cbx] as Dictionary<string, T>;
            cbx.SelectedIndex = extMap.Values.ToList().IndexOf(value);
        }

        public static T ExtGetSelectedValue<T>(this ComboBox cbx)
        {
            var extMap = ComboBoxValueMappings[cbx] as Dictionary<string, T>;
            return extMap[cbx.SelectedValue as string];
        }
    }
}
