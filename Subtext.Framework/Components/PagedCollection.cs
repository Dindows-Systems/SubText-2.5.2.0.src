#region Disclaimer/Info

///////////////////////////////////////////////////////////////////////////////////////////////////
// Subtext WebLog
// 
// Subtext is an open source weblog system that is a fork of the .TEXT
// weblog system.
//
// For updated news and information please visit http://subtextproject.com/
// Subtext is hosted at Google Code at http://code.google.com/p/subtext/
// The development mailing list is at subtext@googlegroups.com 
//
// This project is licensed under the BSD license.  See the License.txt file for more information.
///////////////////////////////////////////////////////////////////////////////////////////////////

#endregion

using System.Collections.Generic;
using Subtext.Extensibility.Interfaces;

namespace Subtext.Framework.Components
{
    /// <summary>
    /// Concrete generic base class for paged collections.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PagedCollection<T> : List<T>, IPagedCollection<T>
    {
        public PagedCollection()
        {
        }

        public PagedCollection(IEnumerable<T> collection) : base(collection)
        {
        }

        /// <summary>
        /// Returns the max number of items to display on a page.
        /// </summary>
        public int MaxItems { get; set; }
    }
}