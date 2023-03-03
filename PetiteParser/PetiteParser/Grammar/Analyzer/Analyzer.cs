﻿using PetiteParser.Formatting;
using PetiteParser.Misc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PetiteParser.Grammar.Analyzer;

/// <summary>This is a tool for calculating the firsts tokens and term sets for a grammar.</summary>
/// <remarks>
/// This performs analysis of the given grammar as it is, any changes to the grammar
/// will require reanalysis. This analysis required propagation through the rules of the grammar
/// meaning that this may be slow for large complex grammars.
/// </remarks>
public class Analyzer {

    /// <summary>Indicates if the grammar has changed and needs refreshed.</summary>
    private bool needsToRefresh;

    /// <summary>The set of groups for all terms in the grammar.</summary>
    private Dictionary<Term, TermData> terms;

    /// <summary>Create a new analyzer which will read from the given grammar.</summary>
    /// <param name="grammar">The grammar to analyze.</param>
    public Analyzer(Grammar grammar) {
        this.needsToRefresh = true;
        this.Grammar = grammar;
        this.terms = new();
    }

    /// <summary>The grammar being analyzed.</summary>
    public Grammar Grammar { get; }

    /// <summary>Updates the analyzed information for the grammar.</summary>
    /// <remarks>This should be called anytime the grammar has been changed so that the data it up-to-date.</remarks>
    public void Refresh() {
        // Even if needsToRefresh is false still refresh in case the grammar was changed outside of the analyzer.
        this.terms = this.Grammar.Terms.ToDictionary(term => term, term => new TermData(t => this.terms[t], term));
        while (this.terms.Values.ForeachAny(group => group.Propagate())) ;
        this.needsToRefresh = false;
    }

    /// <summary>Gets the determined first token sets for the grammar.</summary>
    /// <param name="item">This is the item to get the token set for.</param>
    /// <param name="tokens">The set to add the found tokens to.</param>
    /// <returns>True if the item has a lambda, false otherwise.</returns>
    public bool Firsts(Item item, HashSet<TokenItem> tokens) {
        if (this.needsToRefresh) this.Refresh();

        if (item is TokenItem token) {
            tokens.Add(token);
            return false;
        }

        if (item is Term term) {
            TermData group = this.terms[term];
            group.Tokens.Foreach(tokens.Add);
            return group.HasLambda;
        }

        return false; // Prompt
    }

    /// <summary>Tries to find the first direct or indirect left recursion.</summary>
    /// <returns>The tokens in the loop for the left recursion or null if none.</returns>
    /// <see cref="https://handwiki.org/wiki/Left_recursion"/>
    public List<Term> FindFirstLeftRecursion() {
        if (this.needsToRefresh) this.Refresh();

        TermData? target = this.terms.Values.FirstOrDefault(g => g.LeftRecursive());
        if (target is null) return new List<Term>();

        List<Term> path = new() { target.Term };
        TermData group = target;
        while (true) {
            // If the data propagation worked correctly, then the following exception should never be seen.
            TermData next = group.ChildInPath(target) ??
                throw new GrammarException("No children found in path from " + group.Term +
                    " to " + target.Term + " when left recursive found.");

            if (next == target) return path;
            path.Add(next.Term);
            group = next;
        }
    }

    /// <summary>Determines if the given rule reaches the target term first.</summary>
    /// <param name="rule">The rule to check if the target term is a first.</param>
    /// <param name="target">The target term to check for in the rule.</param>
    /// <returns>True if the target term is first, false otherwise.</returns>
    private bool ruleReaches(Rule rule, Term target) {
        foreach (Item item in rule.BasicItems) {
            if (item is not Term term) return false;
            if (term == target) return true;
            if (!this.terms[term]?.HasLambda ?? false) return false;
        }
        return false;
    }

    /// <summary>Determines the first rule which goes from the parent to the child.</summary>
    /// <param name="parent">The parent to find the rule within.</param>
    /// <param name="child">The child to find the rule to.</param>
    /// <returns>The first rule from the parent to the child or null if none is found.</returns>
    public Rule? FirstRuleBetween(Term parent, Term child) {
        if (this.needsToRefresh) this.Refresh();
        return this.terms[parent]?.Term.Rules.FirstOrDefault(r => this.ruleReaches(r, child));
    }

    /// <summary>Gets a string for debugging the grammar's first tokens.</summary>
    /// <param name="verbose">Shows the children and parent terms.</param>
    /// <returns>The string with the first tokens.</returns>
    public string ToString(bool verbose = false) {
        if (this.needsToRefresh) this.Refresh();
        int maxWidth = this.terms.Keys.Select(term => term.Name.Length).Aggregate(Math.Max);
        string[] parts = this.terms.Values.Select(g => g.ToString(maxWidth, verbose)).ToArray();
        Array.Sort(parts);
        return parts.JoinLines();
    }
}
