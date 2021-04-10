﻿using PetiteParser.Tokenizer;
using System;
using System.Collections.Generic;
using System.Text;

namespace PetiteParser.Parser {

    /// <summary>
    /// This is a parser for running tokens against a grammar to see
    /// if the tokens are part of that grammar.
    /// </summary>
    public class Parser {

        /// <summary>Gets the debug string for the states used for generating the parse table.</summary>
        /// <param name="grammar">The grammar to get the states for.</param>
        /// <returns>The debug string for the parser states.</returns>
        static public string GetDebugStateString(Grammar.Grammar grammar) {
            Builder builder = new(grammar.Copy());
            builder.DetermineStates();
            StringBuilder buf = new();
            foreach (State state in builder.States)
                buf.Append(state.ToString());
            return buf.ToString();
        }

        /// <summary>The parse table to use while parsing.</summary>
        private Table.Table table;

        /// <summary>Creates a new parser with the given grammar.</summary>
        /// <param name="grammar">The grammar for this parser.</param>
        /// <param name="tokenizer">The tokenizer for this parser.</param>
        public Parser(Grammar.Grammar grammar, Tokenizer.Tokenizer tokenizer) {
            string errors = grammar.Validate();
            if (errors.Length > 0)
                throw new Exception("Error: Parser can not use invalid grammar: " + errors);

            grammar = grammar.Copy();
            Builder builder = new(grammar);
            builder.DetermineStates();
            builder.FillTable();
            string errs = builder.BuildErrors;
            if (errs.Length > 0)
                throw new Exception("Errors while building parser:" +
                    Environment.NewLine + builder.ToString(showTable: false));

            this.table = builder.Table;
            this.Grammar = grammar;
            this.Tokenizer = tokenizer;
        }

        /// <summary>Creates a parser from a parser definition file.</summary>
        /// <param name="input">The parser definition.</param>
        public Parser(string input) : this(input.EnumerateRunes()) { }

        /// <summary>Creates a parser from a parser definition string.</summary>
        /// <param name="input">The parser definition.</param>
        public Parser(IEnumerable<Rune> input) {
            Loader loader = new();
            loader.Load(input);
            Parser parser = loader.Parser;

            this.table = parser.table;
            this.Grammar = parser.Grammar;
            this.Tokenizer = parser.Tokenizer;
        }

        /// <summary>
        /// Gets the grammar for this parser.
        /// This should be treated as a constant, modifying it could cause the parser to fail.
        /// </summary>
        public Grammar.Grammar Grammar { get; }

        /// <summary>
        /// Gets the tokenizer for this parser.
        /// This should be treated as a constant, modifying it could cause the parser to fail.
        /// </summary>
        public Tokenizer.Tokenizer Tokenizer { get; }

        /// <summary>This parses the given string and returns the results.</summary>
        /// <param name="input">The input to parse.</param>
        /// <param name="errorCap">The number of errors to allow before failure.</param>
        /// <returns>The result of a parse.</returns>
        public Result Parse(string input, int errorCap = 0) =>
            this.Parse(this.Tokenizer.Tokenize(input), errorCap);

        /// <summary>This parses the given characters and returns the results.</summary>
        /// <param name="runes">The input to parse.</param>
        /// <param name="errorCap">The number of errors to allow before failure.</param>
        /// <returns>The result to parse.</returns>
        public Result Parse(IEnumerable<Rune> runes, int errorCap = 0) =>
          this.Parse(this.Tokenizer.Tokenize(runes), errorCap);

        /// <summary>This parses the given tokens and returns the results.</summary>
        /// <param name="tokens">The input to parse.</param>
        /// <param name="errorCap">The number of errors to allow before failure.</param>
        /// <returns>The result to parse.</returns>
        public Result Parse(IEnumerable<Token> tokens, int errorCap = 0) {
            Runner runner = new(this.table, errorCap);
            foreach (Token token in tokens) {
                if (!runner.Add(token)) return runner.Result;
            }
            runner.Add(new Token(Builder.EofTokenName, Builder.EofTokenName, -1));
            return runner.Result;
        }
    }
}