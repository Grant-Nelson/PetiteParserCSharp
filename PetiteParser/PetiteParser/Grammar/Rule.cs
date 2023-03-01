﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PetiteParser.Grammar;

/// <summary>A rule is a single definition from a grammar.</summary>
/// <remarks>
/// For example `<T> → "(" <E> ")"`. The term for the rule is
/// the left hand side (`T`) while the items are the parts on the right hand size.
/// The items are made up of tokens (`(`, `)`) and the rule's term or other terms (`E`).
/// The order of the items defines how this rule in the grammar is to be used.
/// </remarks>
public partial class Rule : IComparable<Rule> {
    
    /// <summary>The regular expression for breaking up items.</summary>
    [GeneratedRegex("< [^>\\]}]+ > | \\[ [^>\\]}]+ \\] | { [^>\\]}]+ }", RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace)]
    private static partial Regex itemsRegex();

    /// <summary>The grammar this rule belongs too.</summary>
    private readonly Grammar grammar;

    /// <summary>Creates a new rule for the given grammar and term.</summary>
    /// <param name="grammar">The grammar this rule belongs too.</param>
    /// <param name="term">Gets the left hand side term to the rule.</param>
    internal Rule(Grammar grammar, Term term) {
        this.grammar = grammar;
        this.Term = term;
        this.Items = new List<Item>();
    }

    /// <summary>Gets the left hand side term to the rule.</summary>
    public readonly Term Term;

    /// <summary>
    /// Gets all the terms, tokens, and prompts for this rule.
    /// The items are in the order defined by this rule.
    /// </summary>
    public List<Item> Items { get; }

    /// <summary>Adds a term to this rule. This will get or create a new term to the grammar.</summary>
    /// <param name="termName">The name of the term to add.</param>
    /// <returns>This rule so that rule creation can be chained.</returns>
    public Rule AddTerm(string termName) {
        this.Items.Add(this.grammar.Term(termName));
        return this;
    }

    /// <summary> Adds a token to this rule. </summary>
    /// <param name="tokenName">The name of the token.</param>
    /// <returns>This rule so that rule creation can be chained.</returns>
    public Rule AddToken(string tokenName) {
        this.Items.Add(this.grammar.Token(tokenName));
        return this;
    }

    /// <summary>Adds a prompt to this rule.</summary>
    /// <param name="promptName">The name of the prompt</param>
    /// <returns>This rule so that rule creation can be chained.</returns>
    public Rule AddPrompt(string promptName) {
        this.Items.Add(this.grammar.Prompt(promptName));
        return this;
    }

    /// <summary>Adds a list of items to this rule with one or more items in the given string.</summary>
    /// <remarks>
    /// Each item must have a prefix and suffix to indicate which type of item is to be used.
    /// Angle brackets for terms, Square brackets for tokens, and Curly brackets for prompts.
    /// Anything between the items will be ignored.
    /// </remarks>
    /// <param name="items">The items string to add.</param>
    /// <returns>This rule so that rule creation can be chained.</returns>
    public Rule AddItems(string items) {
        MatchCollection matches = itemsRegex().Matches(items);
        foreach (Match match in matches.Cast<Match>()) {
            string text = match.Value.Trim();
            char prefix = text[0];
            string name = text[1..^1];
            if      (prefix == '<') this.AddTerm(name);
            else if (prefix == '[') this.AddToken(name);
            else if (prefix == '{') this.AddPrompt(name);
        }
        return this;
    }

    /// <summary>Gets the set of terms and tokens without the prompts.</summary>
    public IEnumerable<Item> BasicItems =>
        this.Items.Where(item => item is not Prompt);

    /// <summary>Determines if the given rule is equal to this rule.</summary>
    /// <param name="obj">The object to compare against.</param>
    /// <returns>True if they are equal, false otherwise.</returns>
    public override bool Equals(object? obj) {
        if (obj is null) return false;
        if (obj is not Rule other) return false;
        if (this.Term != other.Term) return false;
        if (this.Items.Count != other.Items.Count) return false;
        for (int i = this.Items.Count - 1; i >= 0; i--) {
            if (this.Items[i] != other.Items[i]) return false;
        }
        return true;
    }

    /// <summary>Determines if the given rule is the same as this rule with one term aliased.</summary>
    /// <param name="other">The other rule to check if the same.</param>
    /// <param name="target">The target term to use in place of the alias.</param>
    /// <param name="alias">The alias term to check as the target.</param>
    /// <returns>True if the two rules are the same with the aliased term.</returns>
    internal bool Same(Rule other, Term target, Term alias) {
        if (this.Items.Count != other.Items.Count) return false;
        for (int i = this.Items.Count - 1; i >= 0; i--) {
            Item item1 = this.Items[i];
            Item item2 = other.Items[i];
            if (item1 == alias) item1 = target;
            if (item2 == alias) item2 = target;
            if (item1 != item2) return false;
        }
        return true;
    }

    /// <summary>Compares this rule with the given rule.</summary>
    /// <param name="other">The other rule to compare against.</param>
    /// <returns>
    /// Negative if this rule is smaller than the given other,
    /// 0 if equal, 1 if this rule is larger.
    /// </returns>
    public int CompareTo(Rule other) {
        if (other == null) return 1;
        int cmp = this.Term.CompareTo(other.Term);
        if (cmp != 0) return cmp;
        int count1 = this.Items.Count;
        int count2 = other.Items.Count;
        int min = Math.Min(count1, count2);
        for (int i = 0; i < min; i++) {
            cmp = this.Items[i].CompareTo(other.Items[i]);
            if (cmp != 0) return cmp;
        }
        return count1-count2;
    }

    /// <summary>This gets the hash code for this rule.</summary>
    /// <returns>The base object's hash code.</returns>
    public override int GetHashCode() => base.GetHashCode();

    /// <summary>Gets the string for this rule.</summary>
    /// <returns>The string for this rule.</returns>
    public override string ToString() => this.ToString(-1);

    /// <summary>
    /// Gets the string for this rule.
    /// Has an optional step index for showing the different states of the parser generator.
    /// </summary>
    /// <param name="stepIndex">The index of the current step to show.</param>
    /// <param name="showTerm">Indicates if the term and arrow should be shown at the front of the rule.</param>
    /// <returns>The string for this rule.</returns>
    public string ToString(int stepIndex, bool showTerm = true) {
        StringBuilder buf = new();
        if (showTerm) {
            buf.Append(this.Term.ToString());
            buf.Append(" →");
        }

        int index = 0;
        if (this.Items.Count > 0) {
            foreach (Item item in this.Items) {
                if (index == stepIndex) {
                    buf.Append(" •");
                    stepIndex = -1;
                }
                buf.Append(' ');
                buf.Append(item.ToString());
                if (item is not Prompt) index++;
            }
        } else buf.Append(" λ");
        if (index == stepIndex) buf.Append(" •");
        return buf.ToString();
    }
}
