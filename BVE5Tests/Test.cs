using System;
using System.Collections.Generic;

using NUnit.Framework;

using BVE5Language.Ast;
using BVE5Language.Parser;

namespace BVE5Language.Tests
{
	class TypeDescriber
	{
		private readonly string[] field_names;
		private readonly Type[] types;
		private readonly List<TypeDescriber> children;

		public string[] Names{
			get{return field_names;}
		}

		public Type[] Types{
			get{return types;}
		}

		public List<TypeDescriber> Children{
			get{return children;}
		}

		public TypeDescriber(string[] names, Type[] targetTypes, List<TypeDescriber> inputChildren)
		{
			field_names = names;
			types = targetTypes;
			children = inputChildren;
		}
	}

	[TestFixture]
	public class RouteParserTests
	{
		static Type StmtType = typeof(Statement);
		static Type IdentType = typeof(Identifer);
		static Type IndexerType = typeof(IndexerExpression);
		static Type	InvokeType = typeof(InvocationExpression);
		static Type	LiteralType = typeof(LiteralExpression);
		static Type	MemRefType = typeof(MemberReferenceExpression);
		static Type	TreeType = typeof(SyntaxTree);
		static Type	TimeType = typeof(TimeFormatLiteral);
		static string Basic = 
			@"BveTs Map 1.00
Structure.Load(structures.txt);
Signal.Load(signals.txt);
Signal.SpeedLimit(0, 25, 40, 75, 100,);
Sound.Load(sounds.txt);
Sound3D.Load(sounds3d.txt);
Station.Load(stations.txt);
Train.Add(Oncoming, 3000.txt, 1, -1);
Light.Ambient(0.9375, 0.9375, 0.9375);
//Light.Ambient(1, 1, 1);
Light.Diffuse(0.125, 0.125, 0.125);
//Light.Diffuse(0, 0, 0);
Light.Direction(75, 225);

0;
	Curve.Gauge(1.435);
	Background.Change(Background0);
	Track[Height].Position(0, -0.6);
	Repeater[Gruond].Begin0(Height, 0, 25, 25, Ground0);
	Irregularity.Change(0.002, 0.002, 0.001, 50, 25, 12.5);
	Repeater[Track0].Begin0(0, 3, 5, 5, Ballast0, Ballast1, Ballast2, Ballast3, Ballast4);
	Track[1].Gauge(1.435);
	Track[1].Position(3.4, 0);
	Repeater[Track1].Begin0(1, 3, 5, 5, Ballast0, Ballast1, Ballast2, Ballast3, Ballast4);
	RollingNoise.Change(0);
	FlangeNoise.Change(0);
	Adhesion.Change(0.234, 0, 0.005478851);
50;
	Repeater[Pole0].Begin(0, 0, 0, 0, 0, 0, 0, 1, 25, 50, Pole1_0);
0;
	Fog.Set(0.0004, 0.875, 0.9375, 1);
45;
	Station[sta0].Put(-1, -2, 2);
	Train[Oncoming].Stop(3, 0, 0, 0);
50;	// ======================================== ホーム終端
	CabIlluminance.Set(0.875);
	Structure[Freeobj20].Put(0, -2.8, -0.1, 5, 0, 0, 0, 1, 25);
55;
	Sound3D[crossing].Put(-2.2, 3);
150;
	Sound[nishino1].Play();
125;
	Signal[type0].Put(1, 0, -2.2, 4.5, 20, 0, 0, 0, 1, 25);
145;
	Section.BeginNew(0, 2, 5);";

		private static void AssertType(Type expected, Type actual)
		{
			Assert.IsTrue(actual.FullName != expected.FullName,
			              "Expected the node of type {0} but actually it is of {1}", expected.FullName, actual.FullName);
		}

		private static void TestStructualEqual(List<TypeDescriber> expected, Type expectedRootNodeType, AstNode actual)
		{
			AssertType(expectedRootNodeType, actual.GetType());

			foreach(var expect in expected){

			}
		}

		[Test]
		public void Basics()
		{
			var parser = new BVE5RouteFileParser();
			var stmt = parser.ParseStatement("Sound.Load(sounds.txt);");
			/*var tester1 = new StructualEqualityTester(new List<Type>{
				StmtType,
				InvokeType,
				MemRefType,
				IdentType,
				IdentType,
				IdentType
			});
			stmt.AcceptWalker(tester1);*/
		}
	}
}

