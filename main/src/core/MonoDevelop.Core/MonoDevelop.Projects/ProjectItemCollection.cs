// 
// ProjectItemCollection.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;

namespace MonoDevelop.Projects
{
	public class ProjectItemCollection: ProjectItemCollection<ProjectItem>
	{
		public ProjectItemCollection ()
		{
		}
		
		internal ProjectItemCollection (Project parent): base (parent)
		{
		}
	}

	internal interface IItemListHandler
	{
		void InternalAdd (IEnumerable<ProjectItem> items, bool comesFromParent);
		void InternalRemove (IEnumerable<ProjectItem> items, bool comesFromParent);
		bool CanHandle (ProjectItem item);
	}
	
	public class ProjectItemCollection<T>: ItemCollection<T>, IItemListHandler where T: ProjectItem
	{
		SolutionItem parent;
		IItemListHandler parentCollection;
		List<IItemListHandler> subCollections;
		
		internal ProjectItemCollection (Project parent)
		{
			this.parent = parent;
		}
		
		public ProjectItemCollection ()
		{
		}

		public void Bind<U> (ProjectItemCollection<U> subCollection) where U:T
		{
			AssertCanWrite ();
			if (subCollections == null)
				subCollections = new List<IItemListHandler> ();
			subCollections.Add (subCollection);
			subCollection.parentCollection = this;
			IItemListHandler list = subCollection;
			list.InternalAdd (this.Where (ob => list.CanHandle (ob)), true);
		}
		
		public void Unbind<U> (ProjectItemCollection<U> subCollection) where U:T
		{
			if (subCollections != null) {
				subCollections.Remove (subCollection);
				subCollection.parentCollection = null;
			}
		}
		
		public IEnumerable<U> GetAll<U> () where U:T
		{
			foreach (T it in this) {
				if (it is U)
					yield return (U) it;
			}
		}
		
		protected override void OnItemsAdded (IEnumerable<T> items)
		{
			if (!ignoreChangeEvents) {
				NotifyAdded (items, true);
				NotifyAdded (items, false);
			}
		}
		
		protected override void OnItemsRemoved (IEnumerable<T> items)
		{
			if (!ignoreChangeEvents) {
				NotifyRemoved (items, true);
				NotifyRemoved (items, false);
			}
		}

		bool ignoreChangeEvents;

		void IItemListHandler.InternalAdd (IEnumerable<ProjectItem> items, bool comesFromParent)
		{
			try {
				ignoreChangeEvents = true;
				AddRange (items.Cast<T> ());
			} finally {
				ignoreChangeEvents = false;
			}
			NotifyAdded (items, comesFromParent);
		}
		
		void IItemListHandler.InternalRemove (IEnumerable<ProjectItem> items, bool comesFromParent)
		{
			try {
				ignoreChangeEvents = true;
				RemoveRange (items.Cast<T> ());
			} finally {
				ignoreChangeEvents = false;
			}
			NotifyRemoved (items, comesFromParent);
		}
		
		bool IItemListHandler.CanHandle (ProjectItem obj)
		{
			return obj is T;
		}
		
		void NotifyAdded (IEnumerable<ProjectItem> items, bool comesFromParent)
		{
			if (comesFromParent) {
				if (subCollections != null) {
					foreach (IItemListHandler col in subCollections)
						col.InternalAdd (items.Where (i => col.CanHandle (i)), true);
				}
			} else {
				if (parentCollection != null)
					parentCollection.InternalAdd (items, false);
				if (parent != null)
					parent.OnItemsAdded (items);
			}
		}
		
		void NotifyRemoved (IEnumerable<ProjectItem> items, bool comesFromParent)
		{
			if (comesFromParent) {
				if (subCollections != null) {
					foreach (IItemListHandler col in subCollections)
						col.InternalRemove (items.Where (i => col.CanHandle (i)), true);
				}
			} else {
				if (parentCollection != null)
					parentCollection.InternalRemove (items, false);
				if (parent != null)
					parent.OnItemsRemoved (items);
			}
		}
	}
}
