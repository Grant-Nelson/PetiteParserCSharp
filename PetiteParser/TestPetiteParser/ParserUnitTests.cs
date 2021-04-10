﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using PetiteParser.Grammar;
using PetiteParser.Parser;
using PetiteParser.Tokenizer;
using System;

namespace TestPetiteParser {
    [TestClass]
    public class ParserUnitTests {

        /// <summary>Checks the parser will parse the given input.</summary>
        static private void checkParser(Parser parser, string input, params string[] expected) {
            Result parseResult = parser.Parse(input);
            string exp = string.Join(Environment.NewLine, expected);
            string result = parseResult.ToString();
            Assert.AreEqual(exp, result);
        }

        /// <summary>Checks that an expected error from the parser builder.</summary>
        static private void checkParserBuildError(Grammar grammar, Tokenizer tokenizer, params string[] expected) {
            string exp = string.Join(Environment.NewLine, expected);
            try {
                _ = new Parser(grammar, tokenizer);
                Assert.Fail("Expected an exception from parser builder but got none:"+
                    "\n  Expected: "+exp);
            } catch (Exception err) {
                string result = "Exception: "+err.Message.TrimEnd();
                Assert.AreEqual(exp, result);
            }
        }

        [TestMethod]
        public void Parser1() {
            Tokenizer tok = new();
            tok.Start("start");
            tok.Join("start", "(").AddSet("(");
            tok.Join("start", ")").AddSet(")");
            tok.Join("start", "+").AddSet("+");
            tok.Join("start", "number").AddRange("0", "9");
            tok.Join("number", "number").AddRange("0", "9");
            tok.SetToken("(", "(");
            tok.SetToken(")", ")");
            tok.SetToken("+", "+");
            tok.SetToken("number", "n");
            // 1. E → T
            // 2. E → ( E )
            // 3. T → n
            // 4. T → + T
            // 5. T → T + n
            Grammar grammar = new();
            grammar.Start("E");
            grammar.NewRule("E").AddTerm("T");
            grammar.NewRule("E").AddToken("(").AddTerm("E").AddToken(")");
            grammar.NewRule("T").AddToken("n");
            grammar.NewRule("T").AddToken("+").AddTerm("T");
            grammar.NewRule("T").AddTerm("T").AddToken("+").AddToken("n");
            Parser parser = new(grammar, tok);

            checkParser(parser, "103",
               "─<E>",
               "  └─<T>",
               "     └─[n:3:\"103\"]");

            checkParser(parser, "+2",
               "─<E>",
               "  └─<T>",
               "     ├─[+:1:\"+\"]",
               "     └─<T>",
               "        └─[n:2:\"2\"]");

            checkParser(parser, "3+4",
               "─<E>",
               "  └─<T>",
               "     ├─<T>",
               "     │  └─[n:1:\"3\"]",
               "     ├─[+:2:\"+\"]",
               "     └─[n:3:\"4\"]");

            checkParser(parser, "((42+6))",
               "─<E>",
               "  ├─[(:1:\"(\"]",
               "  ├─<E>",
               "  │  ├─[(:2:\"(\"]",
               "  │  ├─<E>",
               "  │  │  └─<T>",
               "  │  │     ├─<T>",
               "  │  │     │  └─[n:4:\"42\"]",
               "  │  │     ├─[+:5:\"+\"]",
               "  │  │     └─[n:6:\"6\"]",
               "  │  └─[):7:\")\"]",
               "  └─[):8:\")\"]");
        }

        [TestMethod]
        public void Parser2() {
            Tokenizer tok = new();
            tok.Start("start");
            tok.Join("start", "(").AddSet("(");
            tok.Join("start", ")").AddSet(")");
            tok.SetToken("(", "(");
            tok.SetToken(")", ")");
            // 1. X → ( X )
            // 2. X → ( )
            Grammar grammar = new();
            grammar.Start("X");
            grammar.NewRule("X").AddToken("(").AddTerm("X").AddToken(")");
            grammar.NewRule("X").AddToken("(").AddToken(")");            
            Parser parser = new(grammar, tok);
            
            checkParser(parser, "()",
               "─<X>",
               "  ├─[(:1:\"(\"]",
               "  └─[):2:\")\"]");

            checkParser(parser, "((()))",
               "─<X>",
               "  ├─[(:1:\"(\"]",
               "  ├─<X>",
               "  │  ├─[(:2:\"(\"]",
               "  │  ├─<X>",
               "  │  │  ├─[(:3:\"(\"]",
               "  │  │  └─[):4:\")\"]",
               "  │  └─[):5:\")\"]",
               "  └─[):6:\")\"]");
        }

        [TestMethod]
        public void Parser3() {
            Tokenizer tok = new();
            tok.Start("start");
            tok.Join("start", "(").AddSet("(");
            tok.Join("start", ")").AddSet(")");
            tok.SetToken("(", "(");
            tok.SetToken(")", ")");
            // 1. X → ( X )
            // 2. X → 𝜀
            Grammar grammar = new();
            grammar.Start("X");
            grammar.NewRule("X").AddToken("(").AddTerm("X").AddToken(")");
            grammar.NewRule("X");
            Parser parser = new(grammar, tok);

            checkParser(parser, "",
               "─<X>");

            checkParser(parser, "()",
               "─<X>",
               "  ├─[(:1:\"(\"]",
               "  ├─<X>",
               "  └─[):2:\")\"]");

            checkParser(parser, "((()))",
               "─<X>",
               "  ├─[(:1:\"(\"]",
               "  ├─<X>",
               "  │  ├─[(:2:\"(\"]",
               "  │  ├─<X>",
               "  │  │  ├─[(:3:\"(\"]",
               "  │  │  ├─<X>",
               "  │  │  └─[):4:\")\"]",
               "  │  └─[):5:\")\"]",
               "  └─[):6:\")\"]");
        }

        [TestMethod]
        public void Parser4() {
            Tokenizer tok = new();
            tok.Start("start");
            tok.Join("start", "a").AddSet("a");
            tok.Join("start", "b").AddSet("b");
            tok.Join("start", "d").AddSet("d");
            tok.SetToken("a", "a");
            tok.SetToken("b", "b");
            tok.SetToken("d", "d");
            // 1. S → b A d S
            // 2. S → 𝜀
            // 3. A → a A
            // 4. A → 𝜀
            Grammar grammar = new();
            grammar.Start("S");
            grammar.NewRule("S").AddToken("b").AddTerm("A").AddToken("d").AddTerm("S");
            grammar.NewRule("S");
            grammar.NewRule("A").AddToken("a").AddTerm("A");
            grammar.NewRule("A");
            Parser parser = new(grammar, tok);
            
            checkParser(parser, "bd",
               "─<S>",
               "  ├─[b:1:\"b\"]",
               "  ├─<A>",
               "  ├─[d:2:\"d\"]",
               "  └─<S>");

            checkParser(parser, "bad",
               "─<S>",
               "  ├─[b:1:\"b\"]",
               "  ├─<A>",
               "  │  ├─[a:2:\"a\"]",
               "  │  └─<A>",
               "  ├─[d:3:\"d\"]",
               "  └─<S>");

            checkParser(parser, "bdbadbd",
               "─<S>",
               "  ├─[b:1:\"b\"]",
               "  ├─<A>",
               "  ├─[d:2:\"d\"]",
               "  └─<S>",
               "     ├─[b:3:\"b\"]",
               "     ├─<A>",
               "     │  ├─[a:4:\"a\"]",
               "     │  └─<A>",
               "     ├─[d:5:\"d\"]",
               "     └─<S>",
               "        ├─[b:6:\"b\"]",
               "        ├─<A>",
               "        ├─[d:7:\"d\"]",
               "        └─<S>");
        }

        [TestMethod]
        public void Parser5() {
            Tokenizer tok = new();
            tok.Start("start");
            tok.Join("start", "id").AddRange("a", "z");
            tok.Join("id", "id").AddRange("a", "z");
            tok.Join("start", "add").AddSet("+");
            tok.Join("start", "multiply").AddSet("*");
            tok.Join("start", "open").AddSet("(");
            tok.Join("start", "close").AddSet(")");
            tok.Join("start", "start").SetConsume(true).AddSet(" ");
            tok.SetToken("add", "+");
            tok.SetToken("multiply", "*");
            tok.SetToken("open", "(");
            tok.SetToken("close", ")");
            tok.SetToken("id", "id");
            // 1. E → E + E
            // 2. E → E * E
            // 3. E → ( E )
            // 4. E → id
            Grammar grammar = new();
            grammar.Start("E");
            grammar.NewRule("E").AddTerm("E").AddToken("+").AddTerm("E");
            grammar.NewRule("E").AddTerm("E").AddToken("*").AddTerm("E");
            grammar.NewRule("E").AddToken("(").AddTerm("E").AddToken(")");
            grammar.NewRule("E").AddToken("id");
            Parser parser = new(grammar, tok);

            checkParser(parser, "a",
               "─<E>",
               "  └─[id:1:\"a\"]");

            checkParser(parser, "(a + b)",
               "─<E>",
               "  ├─[(:1:\"(\"]",
               "  ├─<E>",
               "  │  ├─<E>",
               "  │  │  └─[id:2:\"a\"]",
               "  │  ├─[+:4:\"+\"]",
               "  │  └─<E>",
               "  │     └─[id:6:\"b\"]",
               "  └─[):7:\")\"]");

            checkParser(parser, "a + b * c",
               "─<E>",
               "  ├─<E>",
               "  │  └─[id:1:\"a\"]",
               "  ├─[+:3:\"+\"]",
               "  └─<E>",
               "     ├─<E>",
               "     │  └─[id:5:\"b\"]",
               "     ├─[*:7:\"*\"]",
               "     └─<E>",
               "        └─[id:9:\"c\"]");

            checkParser(parser, "a + (b * c) + d",
               "─<E>",
               "  ├─<E>",
               "  │  └─[id:1:\"a\"]",
               "  ├─[+:3:\"+\"]",
               "  └─<E>",
               "     ├─<E>",
               "     │  ├─[(:5:\"(\"]",
               "     │  ├─<E>",
               "     │  │  ├─<E>",
               "     │  │  │  └─[id:6:\"b\"]",
               "     │  │  ├─[*:8:\"*\"]",
               "     │  │  └─<E>",
               "     │  │     └─[id:10:\"c\"]",
               "     │  └─[):11:\")\"]",
               "     ├─[+:13:\"+\"]",
               "     └─<E>",
               "        └─[id:15:\"d\"]");
        }

        [TestMethod]
        public void Parser6() {
            Tokenizer tok = new();
            tok.Start("start");
            tok.Join("start", "a").AddSet("a");
            tok.SetToken("a", "a");

            Grammar grammar = new();
            grammar.Start("E");
            grammar.NewRule("E");
            grammar.NewRule("E").AddTerm("E").AddTerm("T");
            grammar.NewRule("T").AddToken("a");
            Parser parser = new(grammar, tok);

            checkParser(parser, "aaa",
               "─<E>",
               "  ├─<E>",
               "  │  ├─<E>",
               "  │  │  ├─<E>",
               "  │  │  └─<T>",
               "  │  │     └─[a:1:\"a\"]",
               "  │  └─<T>",
               "  │     └─[a:2:\"a\"]",
               "  └─<T>",
               "     └─[a:3:\"a\"]");
        }

        [TestMethod]
        public void Parser7() {
            Tokenizer tok = new();
            tok.Start("start");
            tok.JoinToToken("start", "*").AddSet("*");

            Grammar grammar = new();
            grammar.Start("E");
            grammar.NewRule("E");
            grammar.NewRule("E").AddTerm("T").AddTerm("E");
            grammar.NewRule("T").AddToken("*");

            checkParserBuildError(grammar, tok,
               "Exception: Errors while building parser:",
               "state 0:",
               "  <startTerm> → • <E> [eofToken]",
               "  <E> → •",
               "  <E> → • <T> <E>",
               "  <T> → • [*]",
               "  <E>: goto state 1",
               "  <T>: goto state 2",
               "  [*]: goto state 3",
               "state 1:",
               "  <startTerm> → <E> • [eofToken]",
               "state 2:",
               "  <E> → <T> • <E>",
               "  <E> → •",
               "  <E> → • <T> <E>",
               "  <T> → • [*]",
               "  <E>: goto state 4",
               "  <T>: goto state 2",
               "  [*]: goto state 3",
               "state 3:",
               "  <T> → [*] •",
               "state 4:",
               "  <E> → <T> <E> •",
               "",
               "Infinite goto loop found in term T between the state(s) [2].");
        }
    }
}