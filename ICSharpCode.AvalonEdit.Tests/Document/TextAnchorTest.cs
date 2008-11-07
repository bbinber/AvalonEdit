// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <author name="Daniel Grunwald"/>
//     <version>$Revision$</version>
// </file>

using System;
using NUnit.Framework;
using System.Collections.Generic;

namespace ICSharpCode.AvalonEdit.Document.Tests
{
	[TestFixture]
	public class TextAnchorTest
	{
		TextDocument document;
		
		[SetUp]
		public void SetUp()
		{
			document = new TextDocument();
		}
		
		[Test]
		public void AnchorInEmptyDocument()
		{
			TextAnchor a1 = document.CreateAnchor(0);
			TextAnchor a2 = document.CreateAnchor(0);
			a1.MovementType = AnchorMovementType.BeforeInsertion;
			a2.MovementType = AnchorMovementType.AfterInsertion;
			Assert.AreEqual(0, a1.Offset);
			Assert.AreEqual(0, a2.Offset);
			document.Insert(0, "x");
			Assert.AreEqual(0, a1.Offset);
			Assert.AreEqual(1, a2.Offset);
		}
		
		[Test]
		public void AnchorsSurviveDeletion()
		{
			document.Text = new string(' ', 10);
			TextAnchor[] a1 = new TextAnchor[11];
			TextAnchor[] a2 = new TextAnchor[11];
			for (int i = 0; i < 11; i++) {
				//Console.WriteLine("Insert first at i = " + i);
				a1[i] = document.CreateAnchor(i);
				a1[i].SurviveDeletion = true;
				//Console.WriteLine(document.GetTextAnchorTreeAsString());
				//Console.WriteLine("Insert second at i = " + i);
				a2[i] = document.CreateAnchor(i);
				a2[i].SurviveDeletion = false;
				//Console.WriteLine(document.GetTextAnchorTreeAsString());
			}
			for (int i = 0; i < 11; i++) {
				Assert.AreEqual(i, a1[i].Offset);
				Assert.AreEqual(i, a2[i].Offset);
			}
			document.Remove(1, 8);
			for (int i = 0; i < 11; i++) {
				if (i <= 1) {
					Assert.IsFalse(a1[i].IsDeleted);
					Assert.IsFalse(a2[i].IsDeleted);
					Assert.AreEqual(i, a1[i].Offset);
					Assert.AreEqual(i, a2[i].Offset);
				} else if (i <= 8) {
					Assert.IsFalse(a1[i].IsDeleted);
					Assert.IsTrue(a2[i].IsDeleted);
					Assert.AreEqual(1, a1[i].Offset);
				} else {
					Assert.IsFalse(a1[i].IsDeleted);
					Assert.IsFalse(a2[i].IsDeleted);
					Assert.AreEqual(i - 8, a1[i].Offset);
					Assert.AreEqual(i - 8, a2[i].Offset);
				}
			}
		}
		
		
		Random rnd;
		
		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			int seed = Environment.TickCount;
			Console.WriteLine("TextAnchorTest Seed: " + seed);
			rnd = new Random(seed);
		}
		
		[Test]
		public void CreateAnchors()
		{
			List<TextAnchor> anchors = new List<TextAnchor>();
			List<int> expectedOffsets = new List<int>();
			document.Text = new string(' ', 1000);
			for (int i = 0; i < 1000; i++) {
				int offset = rnd.Next(1000);
				anchors.Add(document.CreateAnchor(offset));
				expectedOffsets.Add(offset);
			}
			for (int i = 0; i < anchors.Count; i++) {
				Assert.AreEqual(expectedOffsets[i], anchors[i].Offset);
			}
			GC.KeepAlive(anchors);
		}
		
		[Test]
		public void CreateAndGCAnchors()
		{
			List<TextAnchor> anchors = new List<TextAnchor>();
			List<int> expectedOffsets = new List<int>();
			document.Text = new string(' ', 1000);
			for (int t = 0; t < 250; t++) {
				int c = rnd.Next(50);
				if (rnd.Next(2) == 0) {
					for (int i = 0; i < c; i++) {
						int offset = rnd.Next(1000);
						anchors.Add(document.CreateAnchor(offset));
						expectedOffsets.Add(offset);
					}
				} else if (c <= anchors.Count) {
					anchors.RemoveRange(0, c);
					expectedOffsets.RemoveRange(0, c);
					GC.Collect();
				}
				for (int j = 0; j < anchors.Count; j++) {
					Assert.AreEqual(expectedOffsets[j], anchors[j].Offset);
				}
			}
			GC.KeepAlive(anchors);
		}
		
		[Test]
		public void CreateAndMoveAnchors()
		{
			List<TextAnchor> anchors = new List<TextAnchor>();
			List<int> expectedOffsets = new List<int>();
			document.Text = new string(' ', 1000);
			for (int t = 0; t < 250; t++) {
				//Console.Write("t = " + t + " ");
				int c = rnd.Next(50);
				switch (rnd.Next(4)) {
					case 0:
						//Console.WriteLine("Add c=" + c + " anchors");
						for (int i = 0; i < c; i++) {
							int offset = rnd.Next(document.TextLength);
							TextAnchor anchor = document.CreateAnchor(offset);
							if (rnd.Next(2) == 0)
								anchor.MovementType = AnchorMovementType.BeforeInsertion;
							else
								anchor.MovementType = AnchorMovementType.AfterInsertion;
							anchor.SurviveDeletion = rnd.Next(2) == 0;
							anchors.Add(anchor);
							expectedOffsets.Add(offset);
						}
						break;
					case 1:
						if (c <= anchors.Count) {
							//Console.WriteLine("Remove c=" + c + " anchors");
							anchors.RemoveRange(0, c);
							expectedOffsets.RemoveRange(0, c);
							GC.Collect();
						}
						break;
					case 2:
						int insertOffset = rnd.Next(document.TextLength);
						int insertLength = rnd.Next(1000);
						//Console.WriteLine("insertOffset=" + insertOffset + " insertLength="+insertLength);
						document.Insert(insertOffset, new string(' ', insertLength));
						for (int i = 0; i < anchors.Count; i++) {
							if (anchors[i].MovementType == AnchorMovementType.BeforeInsertion) {
								if (expectedOffsets[i] > insertOffset)
									expectedOffsets[i] += insertLength;
							} else {
								if (expectedOffsets[i] >= insertOffset)
									expectedOffsets[i] += insertLength;
							}
						}
						break;
					case 3:
						int removalOffset = rnd.Next(document.TextLength);
						int removalLength = rnd.Next(document.TextLength - removalOffset);
						//Console.WriteLine("RemovalOffset=" + removalOffset + " RemovalLength="+removalLength);
						document.Remove(removalOffset, removalLength);
						for (int i = anchors.Count - 1; i >= 0; i--) {
							if (expectedOffsets[i] > removalOffset && expectedOffsets[i] < removalOffset + removalLength) {
								if (anchors[i].SurviveDeletion) {
									expectedOffsets[i] = removalOffset;
								} else {
									Assert.IsTrue(anchors[i].IsDeleted);
									anchors.RemoveAt(i);
									expectedOffsets.RemoveAt(i);
								}
							} else if (expectedOffsets[i] > removalOffset) {
								expectedOffsets[i] -= removalLength;
							}
						}
						break;
				}
				Assert.AreEqual(anchors.Count, expectedOffsets.Count);
				for (int j = 0; j < anchors.Count; j++) {
					Assert.AreEqual(expectedOffsets[j], anchors[j].Offset);
				}
			}
			GC.KeepAlive(anchors);
		}
		
		[Test]
		public void RepeatedTextDragDrop()
		{
			document.Text = new string(' ', 1000);
			for (int i = 0; i < 20; i++) {
				TextAnchor a = document.CreateAnchor(144);
				TextAnchor b = document.CreateAnchor(157);
				document.Insert(128, new string('a', 13));
				document.Remove(157, 13);
				a = document.CreateAnchor(128);
				b = document.CreateAnchor(141);
				
				document.Insert(157, new string('b', 13));
				document.Remove(128, 13);
				
				a = null;
				b = null;
				if ((i % 5) == 0)
					GC.Collect();
			}
		}
	}
}
