﻿using PetiteParser.Formatting;
using PetiteParser.Misc;
using PetiteParser.Parser;

namespace PetiteParser.Loader;

/// <summary>The collection of features and the state that they are in.</summary>
public class Features {

    public Features(bool useRegexMatchers = false) =>
        this.useRegexMatchers = useRegexMatchers;

    // Temporary to make the UseRegexMatcher happy.
    private readonly bool useRegexMatchers;

    /// <summary>Indicates that double quote strings in matchers are used as regular expressions.</summary>
    [Name("use_regex_matchers")]
    public bool UseRegexMatchers {
        get => this.useRegexMatchers;
        set {
            if (value) throw new LoaderException("The use_regex_matchers feature is not implemented yet.");
        }
    }

    /// <summary>Indicates that whitespace in regular expressions should be ignored.</summary>
    /// <remarks>This has no effect unless use_regex_matchers is set to true.</remarks>
    [Name("ignore_whitespace_in_regex")]
    public bool IgnoreWhitespaceInRegex { get; set; } = false;

    /// <summary>The string form which indicates how to handle a conflict while creating a parser.</summary>
    /// <remarks>This has no effect until the language has finished being loaded.</remarks>
    [Name("on_conflict")]
    public string OnConflictName {
        get => this.OnConflict.ToString();
        set => this.OnConflict = OnConflict.Find(value) ??
            throw new LoaderException("Unexpected on_conflict value. Expected "+OnConflict.All.Join(", ")+" but got "+value);
    }
    
    /// <summary>Indicates how to handle a conflict while creating a parser.</summary>
    public OnConflict OnConflict = OnConflict.Panic;
}
