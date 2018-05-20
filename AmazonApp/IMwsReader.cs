﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AmazonApp
{
    /// <summary>
    /// Methods to read an object from XML, JSON, or other serial form.
    /// </summary>
    public interface IMwsReader
    {

        /// <summary>
        /// Read the attribute labeled <code>name</code> from the current element
        /// </summary>
        /// <param name="index">The index to get</param>
        /// <returns>the attribute value</returns>
        T ReadAttribute<T>(string name);

        /// <summary>
        /// Read the current child node value as the given type
        /// <para>Throws a <code>SystemException</code> if the value cannot be retrieved as text</para>
        /// </summary>
        /// <param name="name">The name to get</param>
        /// <returns>the text value</returns>
        T ReadValue<T>();

        /// <summary>
        /// Read the labeled value.
        /// Throws a <code></code> if no child node is found with the labeled value of the requested type
        /// <para>For XML reads: <code>&lt;name&gt;value&lt;/name&gt;</code></para>
        /// <para>For JSON reads: <code>name:value</code> if inobjector <code>value</code> if in list</para>
        /// </summary>
        /// <param name="name">The label to lookup.</param>
        /// <returns> The read in value</returns>
        T Read<T>(string name);

        /// <summary>
        /// Read a list of sibling elements
        /// <para>For XML reads: <code>&lt;memberName&gt;value&lt;/memberName&gt;...</code></para>
        /// <para>For JSON reads: <code>name1:[value...]</code> or <code>[value...]</code></para>
        /// </summary>
        /// <param name="name"></param>
        /// <returns>A List</returns>
        List<T> ReadList<T>(string memberName);

        /// <summary>
        /// Read a list of child elements
        /// <para>For XML reads: <code>&lt;name&gt;&lt;memberName&gt;value&lt;/memberName&gt;...&lt;/name&gt;</code></para>
        /// <para>For JSON reads: <code>name1:[value...]</code> or <code>[value...]</code></para>
        /// </summary>
        /// <param name="name"></param>
        /// <returns>A List</returns>
        List<T> ReadList<T>(string name, string memberName);

        /// <summary>
        /// Read a list of all elements, ignoring type and name.
        /// </summary>
        /// <returns>A List of w3c DOM Elements</returns>
        List<XmlElement> ReadAny();
    }
}
