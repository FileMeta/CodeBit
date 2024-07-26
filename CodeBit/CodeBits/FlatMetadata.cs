using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileMeta
{
    /// <summary>
    /// <para>A object model containing flat metadata - that is properties consisting of
    /// names and values. Interpretation of the values is the responsibility of the
    /// user of this class. Therefore this class keeps all values in string form.
    /// Some properties may have multiple values so this class treats those as
    /// an ordered list of values.
    /// </para>
    /// <para>The metadata record is flat in the sense that there are no "object" values
    /// as might be found in JSON with their own collection of names and values.
    /// </para>
    /// <para>Whether a property is single-valued or multiple-valued depends on how the
    /// caller treats the property. Use <see cref="GetValue"/> and <see cref="SetValue"/>
    /// for single-valued properties. Use <see cref="GetValues"/>, <see cref="SetValues"/>,
    /// <see cref="AddValue"/>, and <see cref="AddValues"/> for multi-valued properties.
    /// </para>
    /// <para>A null value and absence of a value are treated the same. Lists of
    /// values SHOULD NOT include null among the values. Setting a value to
    /// null using <see cref="SetValue"/> will result in removal of the value.
    /// Including null in a list of values sent to <see cref="SetValues"/> or
    /// <see cref="AddValues"/> will result in an <see cref="ArgumentNullException"/>.
    /// However, by manipulating the list returned from <see cref="GetValues"/>, the
    /// caller can still insert a null value or create a list of zero values. The caller
    /// SHOULD NOT do so but the class will tolerate those cases.
    /// </para>
    /// </summary>
    class FlatMetadata : Dictionary<string, List<string>>
    {
        /// <summary>
        /// Gets the value of a single-valued property.
        /// </summary>
        /// <param name="key">Name of the property to return.</param>
        /// <returns>The value of the property or null if it doesnt exist. If multiple values
        /// were set for the property returns the first one.</returns>
        public string? GetValue(string key)
        {
            List<string>? list;
            if (TryGetValue(key, out list) && list.Count > 0) return list[0];
            return null;
        }

        /// <summary>
        /// Returns a list of all values for a property.
        /// </summary>
        /// <param name="key">Name of the property to return.</param>
        /// <param name="createIfNotPresent">Create a list if the property is not present.</param>
        /// <returns>A list of values or null if not present and createIfNotPresent is false.</returns>
        /// <remarks>
        /// <para>If the property is not present, and <paramref name="createIfNotPresent"/> is true
        /// then the property will be created with an empty list for the value. In that case, the
        /// caller SHOULD immediately add a value to the property since an empty list is not
        /// a proper state. Nevertheless that state is tolerated by the class so long as the
        /// calling application also tolerates the state of having an empty list of values
        /// for a property.
        /// </para>
        /// </remarks>
        public IList<string>? GetValues(string key)
        {
            List<string>? list;
            if (TryGetValue(key, out list)) return list;
            return null;
        }

        /// <summary>
        /// Returns a list of all values for a property.
        /// </summary>
        /// <param name="key">Name of the property to return.</param>
        /// <returns>A list of values.</returns>
        /// <remarks>
        /// <para>If the property is not present then the property will be created with an
        /// empty list for the value. In that case, the caller SHOULD immediately add a value
        /// to the property since an empty list is not a proper state. Nevertheless that state
        /// is tolerated by the class so long as the calling application also tolerates the
        /// state of having an empty list of values for a property.
        /// </para>
        /// </remarks>
        public IList<string> GetValuesAlways(string key)
        {
            List<string>? list;
            if (TryGetValue(key, out list)) return list;
            list = new List<string>();
            Add(key, list);
            return list;
        }

        /// <summary>
        /// Sets the value of a single-valued property.
        /// </summary>
        /// <param name="key">Name of the property to set.</param>
        /// <param name="value">Value to be set.</param>
        public void SetValue(string key, string value)
        {
            var list = GetValuesAlways(key);
            list.Clear();
            list.Add(value);
        }

        /// <summary>
        /// Set the value of a property to a provided list of values.
        /// </summary>
        /// <param name="key">Name of the property to set</param>
        /// <param name="values">Values to be set</param>
        /// <exception cref="ArgumentNullException">Thrown if values contains a null.</exception>
        /// <remarks>
        /// <para>Any existing values are replaced.
        /// </para>
        /// <para>If values is null or if it is an empty list. The property is removed
        /// </para>
        /// </remarks>
        public void SetValues(string key, IEnumerable<string> values)
        {
            // Check for null
            if (values == null)
            {
                Remove(key);
                return;
            }
            foreach(var value in values)
            {
                if (value == null) throw new ArgumentNullException("Values may not contain null.");
            }
            var list = (List<string>)GetValuesAlways(key);
            list.Clear();
            list.AddRange(values);
            if (list.Count == 0)
            {
                Remove(key);
            }
        }

        /// <summary>
        /// Add a value to a property.
        /// </summary>
        /// <param name="key">Name of the property to which to add a value.</param>
        /// <param name="value">Value to be added.</param>
        /// <returns>The number of values of the property.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the value is null.</exception>
        /// <remarks>
        /// <para>If the property does not yet exist it will be created and the one value
        /// will be added. This is helpful parsers and reading applications where it's not
        /// known whether the property should be single or multi-valued.
        /// </para>
        /// </remarks>
        public int AddValue(string key, string value)
        {
            if (value == null) throw new ArgumentNullException("Value cannot be null. Use Remove(string) to remove a value.");
            var list = GetValuesAlways(key);
            list.Add(value);
            return list.Count;
        }

        /// <summary>
        /// Add a collection of values to the values of a property.
        /// </summary>
        /// <param name="key">Name of the property to set</param>
        /// <param name="values">Values to be added</param>
        /// <exception cref="ArgumentNullException">Thrown if values is null or any value in the values list is null.</exception>
        /// <remarks>
        /// <para>The provided values are added to any existing values. If there
        /// are no existing values the value is set to the provided list
        /// </para>
        /// </remarks>
        public int AddValues(string key, IEnumerable<string> values)
        {
            // Check for null
            if (values == null) throw new ArgumentNullException("Values may not be null.");
            foreach (var value in values)
            {
                if (value == null) throw new ArgumentNullException("Values may not contain null.");
            }
            var list = (List<string>)GetValuesAlways(key);
            list.AddRange(values);
            return list.Count;
        }

        /// <summary>
        /// Gets a value as a <see cref="DateTimeOffset"/>
        /// </summary>
        /// <param name="key">The name of the value to return.</param>
        /// <returns>The value as a DateTimeOffset if present and valid. Otherwise
        /// <see cref="DateTimeOffset.MinValue"/>.</returns>
        public DateTimeOffset GetValueAsDate(string key)
        {
            var strValue = GetValue(key);
            if (strValue != null && DateTimeOffset.TryParse(strValue, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.RoundtripKind,
                out DateTimeOffset value)) return value;
            return DateTimeOffset.MinValue;
        }

        /// <summary>
        /// Sets a value from a <see cref="DateTimeOffset"/>
        /// </summary>
        /// <param name="key">The name of the value to set.</param>
        /// <param name="value">The value to be set.</param>
        /// <remarks>
        /// <para>If <paramref name="value"/> is <see cref="DateTimeOffset.MinValue"/> then
        /// the property is removed from the collection.
        /// </para>
        /// </remarks>
        public void SetValue(string key, DateTimeOffset value)
        {
            if (value == DateTimeOffset.MinValue)
            {
                Remove(key);
                return;
            }
            var strValue = (value.TimeOfDay.Ticks == 0)
                ? value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : value.ToString("yyyy-MM-ddTHH:mm:ss.FFFzzz", CultureInfo.InvariantCulture);
            SetValue(key, strValue);
        }
    }
}
