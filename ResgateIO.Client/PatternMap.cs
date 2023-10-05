using System;
using System.Collections.Generic;
using System.Text;

namespace ResgateIO.Client
{
    internal class PatternMap<T>
    {
		private class Node
        {
			public Dictionary<string, Node> Nodes;
			public Node PartialWildcard;
			public Node FullWildcard;
			public bool IsSet;
			public T Value;
        }

		private Node root;

		public PatternMap()
        {
			this.root = new Node();
        }

        public void Add(string pattern, T value)
        {
			string[] tokens = pattern.Split('.');
			Node l = root;
			Node n;
			bool sfwc = false;

			foreach (string t in tokens)
			{
				var lt = t.Length;
				if (lt == 0 || sfwc)
				{
					throw new ArgumentException("Invalid pattern");
				}

				if (lt > 1)
				{
					if (l.Nodes != null)
					{
						if (!l.Nodes.TryGetValue(t, out n))
                        {
							n = new Node();
							l.Nodes.Add(t, n);
                        }
					}
					else
					{
						n = new Node();
						l.Nodes = new Dictionary<string, Node>() { { t, n } };
					}
				}
				else
				{
					if (t[0] == '*')
					{
						if (l.PartialWildcard == null)
                        {
							l.PartialWildcard = new Node();
                        }
						n = l.PartialWildcard;
					}
					else if (t[0] == '>')
					{
						if (l.FullWildcard == null)
                        {
							l.FullWildcard = new Node();
                        }
						n = l.FullWildcard;
						sfwc = true;
					}
					else if (l.Nodes != null)
					{
						if (!l.Nodes.TryGetValue(t, out n))
                        {
							n = new Node();
							l.Nodes.Add(t, n);
                        }
					}
					else
					{
						n = new Node();
						l.Nodes = new Dictionary<string, Node>() { { t, n } };
					}
				}
				l = n;
			}

			if (l.IsSet)
			{
				throw new InvalidOperationException("Pattern already registered");
			}

			l.Value = value;
			l.IsSet = true;
		}

		public bool TryGet(string rid, out T value)
		{
			// Remove any query part of the resource ID
			int idx = rid.IndexOf("?");
			if (idx >= 0)
			{ 
				rid = rid.Substring(0, idx);
			}
			if (rid != "")
			{
				string[] tokens = rid.Split('.');
				Node n = match(tokens, 0, root);
				if (n != null)
				{
					value = n.Value;
					return true;
				}
			}
			value = default(T);
			return false;
		}

		private Node match(string[] ts, int i, Node l)
        {
			string t = ts[i++];
			Node n = l.Nodes?[t];
			int c = 2;
			while (c > 0)
			{
				if (n != null)
				{
					if (ts.Length == i)
					{
						return n.IsSet ? n : null;
					}
					else
					{
						Node m = this.match(ts, i, n);
						if (m != null)
						{
							return m;
						}
					}
				}
				n = l.PartialWildcard;
				c--;
			}
			return l.FullWildcard != null && l.FullWildcard.IsSet
				? l.FullWildcard
				: null;
		}

	}
}
