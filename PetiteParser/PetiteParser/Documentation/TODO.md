﻿# TODO

This is the list of new features, improvements, and issues that need to be worked on.

## Keep Locations

The states, tokens, rules, and transitions, should carry the location in the language definition
that they were defined on so that errors can reference the language definition while creating
the parser table and for debugging.

## Parser Parser Version Or Feature Selection

May want to add a header to language definitions to select the version of the laguage definition
parser to use. That way new features can be added to the parser and it won't break the customers'
language definitions because they will sepecify the version to use. The version could select unique
or older parser or the version could simply enable/disable features in groups (see below).

Alternitively the header to language definitions could be a set of features which the customer wants
to enable or disable. For example if they want to enable double quotes to do sequence matcher instead
of be the same as the current matching system. This would allow the customer to always use the newest
parser with all bug fixes and allow only one parser to be defined but will keep from breaking
customers which used something which collides with newer features.

## Serialization

Need to add serialization to make reloading parser languages faster than computing it from the
language definition.

May want to add a basic CRC or hash of the language definition into the serialization so that the
language definition and serialization can be given on load time and the loader can determine if the
language definition has changed. Then it can recompute the parser instead of reloading from
serialization.

## Matcher Simplification

Need to add a way to simplify and reduce a group of matchers to the best matcher. The best matcher
doesn't have to be the smallest matcher, it should be the fastest to perform, which is likely also
the smallest.

## Rework The Tokenizer

There are several improvements which work together to create an overall easier to use language for
the language definitions. [Automatic State Generation](#automatic_state_generation) is a minimum
requirement for any of the features for this improvement.

### Automatic State Generation

Add a way to automatically create tokenizer states with unique name. The language definition can
override that unique name with a specific name, meaning that the state needs to keep track of if it
was automatically created or not.

The reason to do this is so that the language definition can add sequence matching like
`(Start): @"If" => (If);`. This would create the same as `(Start): 'I' => (Auto-1): 'f' => (If);`.
During creation that automatically state (or specificly named state) has to be looked up such that
if `(Start): @"Is" => (Is);` will reuse `(Auto-1)`. That way there isn't a conflict between two
states rechable from `(Start)` with 'I'.

This would also need to be able to split a state into two when needing to make a transition group of
characters more specific. For example if we were given `(Start): 'a'..'z' => (Id): 'a'..'z' => (Id);`
then given `(Start): @"Then" => (Then);` the `(Id)` node will have to be split several times. That
way if "Ta" is given it will start heading down the automatically created states for "Then" but will
transition back to the `(Id)` states when "a" is read.

**Note:** Not set on the `@`, may want to require that all character matchers use single quotes and
all sequence matchers use double quotes. Since right now single quotes and double quotes are
interchangable, this could break consumers' language defitions. Also, any regular expression or
sequence matchers should not be able to be used in ranges or OR-ed with other character matchers.
(ie. this is not allowed `(Start): @"Then", 'a'..'n' => (What)`).

### Regular Expressions and Predefined Character Groups

Once automatic state generation works basic regular expressions can be added. The regular expression
should be similar to the C# regular expressions except without the matching parts. A language
specific for parsing the regular expression will be needed so that it can be turned into matchers and
states. An example one could look like `(Start): @"A (B|C)+ D? E{2,3}" => (Chatter)`. Whitespace
should be ignored and  the `\` special matches should used the predefined character groups in
`Predef`.

The way to start a regular expression could be the same as entering squence matcher since the only
difference is the branching in the regular expression. This does mean that it could break customers'
language defitions if they had a sequence like `"make()"` with regular expression operators already
in them.

### Token Value Matchers in Rule

Need to add a way to allow specific value tokens to be used in a rule without requiring a custom
token to be explicitly defined. This might be able to be done with each token having multiple keys.
The value should be able to specified with regular expressions as well, leveraging the
[Regular Expressions](#regular_expressions_and_predefined_character_groups) feature. For example
`<Statement> := "int|float" <Term>;`. 

The tokenizer might have to produce a special token keys for any of these autogenerated tokens so
that the CRL(1) can determine which rules to take, this is why
[Multiple Token Keys](#multiple_token_keys) might be needed. The idea is that when matching a follow
any of the keys in the token work instead of the single token name. For example `"this"` maybe an
`[ID]` in most cases but also be used in one specific place so needs a special token key like
`[Auto-1]`. This would work well with [Multiple Tokens In Rule](#multiple_tokens_in_rule).

## Multiple Token Keys

Need to add a way to have multiple keys on a token. Right now the token `[Id]` only has the key
`[Id]`. This will allow multiple rules to use different keys from the same tokenizer state. For
example `(Start): 'a'..'z' => [Id, Char, Other];` could be used in `<S> := [Id] <T> | <S> [Char] <T>;`.
This does mean that the when picking a rule from the table, the token needs to look for multiple
matching rules and throw exception if multiple rules match, otherwise when creating the table all
the tokens which are seen together need to be checked for conflict. Prechecking for conflicts would
be more difficult but would be better so the language designer gets notified of the conflict instead
of the user of the language.

### Multiple Tokens In Rule

Some rules are nearly identical with only the token being different. To make defining these rules
easier the tokens in the rules should be allowed to have multiple keys similar to 
[Multiple Token Keys](#multiple_token_keys). For example `<S> := [Id, Char, Other] <T>;`.
This should be handled by creating multiple rules for each combination of token keys.
For example `<S> := [A, B] <T> [C, D];` will expand to
`<S> := [A] <T> [C] | [A] <T> [D] | [B] <T> [C] | [B] <T> [D];`.

### Group Start States

Some tokenizer states lead to another state with the same transition or leads to the same token.
For example `(A, B, C): '&' => (D);` or `(A, B, c) => [Int]`. This should be handled as if it was
the same as all of those being individual state definitions.

This only works at the start of the state since something like `(A): 'a'..'c' => (A, B, C)`
would work, only the first state would be taken and the rest would be unreachable from this
transition.

## Parse Errors

Need to add a simple way to add errors into the parse table via the language definition so that the
language can add suggestions for failures or indication of unsupported or future features of the
language.

For example if you can't have a `[Dash]` followed by a `[RArrow]` yet but you plan to in the future,
it is a deprecated feature, or a common mistake then you could add something like the following to
the grammar, `<Statement> := [Dash] [LArrow] <Number> | [Dash] [RArrow] Err: "May not use -> yet!"`.