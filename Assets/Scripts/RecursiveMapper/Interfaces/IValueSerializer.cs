using System;

namespace RecursiveMapper
{
    /// <summary>
    /// Provides a strategy of value serialization.
    /// </summary>
    public interface IValueSerializer
    {
        /// <summary>
        /// Represents object as a string value.
        /// </summary>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        string Serialize<T>(T value);

        /// <summary>
        ///
        /// </summary>
        /// <param name="type"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        object Deserialize(Type type, string text);
    }
}