
using System;
using System.Collections.Generic;
#if NET451
using System.ComponentModel;
#endif
using System.Linq;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.DecisionTree;

namespace Handlebars.WebApi
{

    /// <inheritdoc />
    public class HandlebarsActionSelectionDecisionTree : IActionSelectionDecisionTree
    {
        

        private readonly DecisionTreeNode<ActionDescriptor> _root;

        /// <summary>
        /// Creates a new <see cref="ActionSelectionDecisionTree"/>.
        /// </summary>
        /// <param name="actions">The <see cref="ActionDescriptorCollection"/>.</param>
        public HandlebarsActionSelectionDecisionTree(ActionDescriptorCollection actions)
        {
            Version = actions.Version;

            var conventionalRoutedActions = actions.Items.ToArray();
            _root = DecisionTreeBuilder<ActionDescriptor>.GenerateTree(
                conventionalRoutedActions,
                new ActionDescriptorClassifier());
        }



        /// <inheritdoc />
        public int Version { get; private set; }

        /// <inheritdoc />
        public IReadOnlyList<ActionDescriptor> Select(IDictionary<string, object> routeValues)
        {
            var results = new List<ActionDescriptor>();
            Walk(results, routeValues, _root);

            return results;
        }

        private void Walk(
            List<ActionDescriptor> results,
            IDictionary<string, object> routeValues,
            DecisionTreeNode<ActionDescriptor> node)
        {
            for (var i = 0; i < node.Matches.Count; i++)
            {
                results.Add(node.Matches[i]);
            }

            for (var i = 0; i < node.Criteria.Count; i++)
            {
                var criterion = node.Criteria[i];
                var key = criterion.Key;

                object value;
                routeValues.TryGetValue(key, out value);

                DecisionTreeNode<ActionDescriptor> branch;
                if (criterion.Branches.TryGetValue(value ?? string.Empty, out branch))
                {
                    Walk(results, routeValues, branch);
                }
            }
        }

        private class ActionDescriptorClassifier : IClassifier<ActionDescriptor>
        {
            public ActionDescriptorClassifier()
            {
                ValueComparer = new RouteValueEqualityComparer();
            }

            public IEqualityComparer<object> ValueComparer { get; private set; }

            public IDictionary<string, DecisionCriterionValue> GetCriteria(ActionDescriptor item)
            {
                var results = new Dictionary<string, DecisionCriterionValue>(StringComparer.OrdinalIgnoreCase);

                if (item.RouteValues != null)
                {
                    foreach (var kvp in item.RouteValues)
                    {
                        // null and string.Empty are equivalent for route values, so just treat nulls as
                        // string.Empty.
                        results.Add(kvp.Key, new DecisionCriterionValue(kvp.Value ?? string.Empty));
                    }
                }

                return results;
            }
        }
    }

    internal class DecisionCriterion<TItem>
    {
        public string Key { get; set; }

        public Dictionary<object, DecisionTreeNode<TItem>> Branches { get; set; }
    }

    internal interface IClassifier<TItem>
    {
        IDictionary<string, DecisionCriterionValue> GetCriteria(TItem item);

        IEqualityComparer<object> ValueComparer { get; }
    }

    internal class DecisionCriterionValueEqualityComparer : IEqualityComparer<DecisionCriterionValue>
    {
        public DecisionCriterionValueEqualityComparer(IEqualityComparer<object> innerComparer)
        {
            InnerComparer = innerComparer;
        }

        public IEqualityComparer<object> InnerComparer { get; private set; }

        public bool Equals(DecisionCriterionValue x, DecisionCriterionValue y)
        {
            return InnerComparer.Equals(x.Value, y.Value);
        }

        public int GetHashCode(DecisionCriterionValue obj)
        {
            return InnerComparer.GetHashCode(obj.Value);
        }
    }

    internal struct DecisionCriterionValue
    {
        private readonly object _value;

        public DecisionCriterionValue(object value)
        {
            _value = value;
        }

        public object Value
        {
            get { return _value; }
        }
    }

    // Data structure representing a node in a decision tree. These are created in DecisionTreeBuilder
    // and walked to find a set of items matching some input criteria.
    internal class DecisionTreeNode<TItem>
    {
        // The list of matches for the current node. This represents a set of items that have had all
        // of their criteria matched if control gets to this point in the tree.
        public IList<TItem> Matches { get; set; }

        // Additional criteria that further branch out from this node. Walk these to fine more items
        // matching the input data.
        public IList<DecisionCriterion<TItem>> Criteria { get; set; }
    }

    internal class ItemDescriptor<TItem>
    {
        public IDictionary<string, DecisionCriterionValue> Criteria { get; set; }

        public int Index { get; set; }

        public TItem Item { get; set; }
    }

    internal static class DecisionTreeBuilder<TItem>
    {
        public static DecisionTreeNode<TItem> GenerateTree(IReadOnlyList<TItem> items, IClassifier<TItem> classifier)
        {
            var itemDescriptors = new List<ItemDescriptor<TItem>>();
            for (var i = 0; i < items.Count; i++)
            {
                itemDescriptors.Add(new ItemDescriptor<TItem>()
                {
                    Criteria = classifier.GetCriteria(items[i]),
                    Index = i,
                    Item = items[i],
                });
            }

            var comparer = new DecisionCriterionValueEqualityComparer(classifier.ValueComparer);
            return GenerateNode(
                new TreeBuilderContext(),
                comparer,
                itemDescriptors);
        }

        private static DecisionTreeNode<TItem> GenerateNode(
            TreeBuilderContext context,
            DecisionCriterionValueEqualityComparer comparer,
            IList<ItemDescriptor<TItem>> items)
        {
            // The extreme use of generics here is intended to reduce the number of intermediate
            // allocations of wrapper classes. Performance testing found that building these trees allocates
            // significant memory that we can avoid and that it has a real impact on startup.
            var criteria = new Dictionary<string, Criterion>(StringComparer.OrdinalIgnoreCase);

            // Matches are items that have no remaining criteria - at this point in the tree
            // they are considered accepted.
            var matches = new List<TItem>();

            // For each item in the working set, we want to map it to it's possible criteria-branch
            // pairings, then reduce that tree to the minimal set.
            foreach (var item in items)
            {
                var unsatisfiedCriteria = 0;

                foreach (var kvp in item.Criteria)
                {
                    // context.CurrentCriteria is the logical 'stack' of criteria that we've already processed
                    // on this branch of the tree.
                    if (context.CurrentCriteria.Contains(kvp.Key))
                    {
                        continue;
                    }

                    unsatisfiedCriteria++;

                    Criterion criterion;
                    if (!criteria.TryGetValue(kvp.Key, out criterion))
                    {
                        criterion = new Criterion(comparer);
                        criteria.Add(kvp.Key, criterion);
                    }

                    List<ItemDescriptor<TItem>> branch;
                    if (!criterion.TryGetValue(kvp.Value, out branch))
                    {
                        branch = new List<ItemDescriptor<TItem>>();
                        criterion.Add(kvp.Value, branch);
                    }

                    branch.Add(item);
                }

                // If all of the criteria on item are satisfied by the 'stack' then this item is a match.
                if (unsatisfiedCriteria == 0)
                {
                    matches.Add(item.Item);
                }
            }

            // Iterate criteria in order of branchiness to determine which one to explore next. If a criterion
            // has no 'new' matches under it then we can just eliminate that part of the tree.
            var reducedCriteria = new List<DecisionCriterion<TItem>>();
            foreach (var criterion in criteria.OrderByDescending(c => c.Value.Count))
            {
                var reducedBranches = new Dictionary<object, DecisionTreeNode<TItem>>(comparer.InnerComparer);

                foreach (var branch in criterion.Value)
                {
                    var reducedItems = new List<ItemDescriptor<TItem>>();
                    foreach (var item in branch.Value)
                    {
                        if (context.MatchedItems.Add(item))
                        {
                            reducedItems.Add(item);
                        }
                    }

                    if (reducedItems.Count > 0)
                    {
                        var childContext = new TreeBuilderContext(context);
                        childContext.CurrentCriteria.Add(criterion.Key);

                        var newBranch = GenerateNode(childContext, comparer, branch.Value);
                        reducedBranches.Add(branch.Key.Value, newBranch);
                    }
                }

                if (reducedBranches.Count > 0)
                {
                    var newCriterion = new DecisionCriterion<TItem>()
                    {
                        Key = criterion.Key,
                        Branches = reducedBranches,
                    };

                    reducedCriteria.Add(newCriterion);
                }
            }

            return new DecisionTreeNode<TItem>()
            {
                Criteria = reducedCriteria.ToList(),
                Matches = matches,
            };
        }

        private class TreeBuilderContext
        {
            public TreeBuilderContext()
            {
                CurrentCriteria = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                MatchedItems = new HashSet<ItemDescriptor<TItem>>();
            }

            public TreeBuilderContext(TreeBuilderContext other)
            {
                CurrentCriteria = new HashSet<string>(other.CurrentCriteria, StringComparer.OrdinalIgnoreCase);
                MatchedItems = new HashSet<ItemDescriptor<TItem>>();
            }

            public HashSet<string> CurrentCriteria { get; private set; }

            public HashSet<ItemDescriptor<TItem>> MatchedItems { get; private set; }
        }

        // Subclass just to give a logical name to a mess of generics
        private class Criterion : Dictionary<DecisionCriterionValue, List<ItemDescriptor<TItem>>>
        {
            public Criterion(DecisionCriterionValueEqualityComparer comparer)
                : base(comparer)
            {
            }
        }
    }
}