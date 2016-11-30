using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Dac.Extensions.Prototype;
using Microsoft.SqlServer.Dac.Model;
using Shields.GraphViz.Models;
using Shields.GraphViz.Services;
using Shields.GraphViz.Components;

namespace StronglyTypedDacFxSample
{
    class Program
    {
        static void Main(string[] args)
        {

            var model = new TSqlTypedModel(@"..\..\..\SampleDacPac\pubs.dacpac");

            string fmt = "PlantUML"; // "gVizNative"
            //var s = docdbtest(model);

            var modeldef = "";

            if (fmt == "PlantUML")
            {
                modeldef = @"@startuml
                    !define table(x) class x << (T,mistyrose) >>      
                    !define view(x) class x << (V,lightblue) >>      
                    !define table(x) class x << (T,mistyrose) >>     
                    !define tr(x) class x << (R,red) >>     
                    !define tf(x) class x << (F,darkorange) >>      
                    !define af(x) class x << (F,white) >>      
                    !define fn(x) class x << (F,plum) >>      
                    !define fs(x) class x << (F,tan) >>       
                    !define ft(x) class x << (F,wheat) >>      
                    !define if(x) class x << (F,gaisboro) >>      
                    !define p(x) class x << (P,indianred) >>      
                    !define pc(x) class x << (P,lemonshiffon) >>      
                    !define x(x) class x << (P,linen) >>          

                    hide methods      
                    hide stereotypes     
                    skinparam classarrowcolor gray          
                    ";

            }
            else if (fmt == "gVizNative")
            {
                modeldef = @"digraph G { 
                        //  
                        // Defaults
                        //  
 
                        // Box for entities
                        node [shape=none, margin=0]
 
                        // One-to-many relation (from one, to many)
                        edge [arrowhead=crow, arrowtail=none, dir=both]";
            }

            var s = OutputDiagramSchemaDef(model,fmt);

            System.Console.WriteLine("///////////////////////////////////////////////////////////////////////////////////");

            // really need to tie all the strings together
            System.Console.WriteLine(modeldef);
            System.Console.Write(s);
            System.Console.WriteLine("");
            OutputDiagramRelationships(model,fmt);

            var modelfooter = "";

            if (fmt == "PlantUML")
            {
                modelfooter = "\n@enduml";
            }
            else if (fmt == "gVizNative")
            {
                modelfooter = "\n}";
            }
            System.Console.WriteLine(modelfooter);

                GraphVizTest(model);


            System.Console.ReadKey();



        }


        private static string removeQualifiers(string inputString)
        {
            return (inputString.Replace("[", "").Replace("]", ""));

        }

        private static string docdbtest(TSqlTypedModel model)
        {
            var tables = model.GetObjects<TSqlTable>(DacQueryScopes.UserDefined);

            foreach (var t in tables)
            {

                var objects = t.GetChildren();
                System.Console.WriteLine("===============================================");
                System.Console.WriteLine("{0}",t.Name.ToString());
                System.Console.WriteLine("-----------------------------------------------");


                foreach (var o in objects)
                {
                    if (o.ObjectType.Name == "DefaultConstraint")
                    {
                        var expression = o.GetProperty(DefaultConstraint.Expression);

                        System.Console.WriteLine("{0}, {1}", "DEFAULT", expression);          
                      //foreach(var g in DefaultConstraint.TypeClass.Properties)
                      //  {
                      //      System.Console.WriteLine(" default constraint prop name: {0}", g.Name);
                      //  }
                      
                        //o.GetProperty<DefaultConstraint>();
                        //System.Console.WriteLine("");
                    }
                    
                    if (o.ObjectType.Name == "ForeignKeyConstraint")
                    {

                        var ft = o.GetReferenced(ForeignKeyConstraint.ForeignTable);
                        var fcs = o.GetReferenced(ForeignKeyConstraint.ForeignColumns);

                        foreach (var f in ft)
                        {
                            System.Console.Write("Foreign Table {0} ", f.Name);
                        }
                        foreach (var fc in fcs)
                        {
                            System.Console.WriteLine("Foreign Column {0}", fc.Name);
                        }
                        foreach (var g in ForeignKeyConstraint.TypeClass.Properties)
                        {
                            System.Console.WriteLine(" FK constraint prop name: {0}", g.Name);
                        }

                    }

                    if (o.ObjectType.Name != "Column")
                    {
                      
                        System.Console.WriteLine("\t*\t{0} --> {1} {2}", o.Name.ToString(), o.ObjectType.Name, o.Name.ExternalParts);
                    }
                }
            }

            return "";
        }

        // loop over schema and call the individual table code to output the table def
        private static string OutputDiagramSchemaDef(TSqlTypedModel model,string outputFormat)
        {
            var schemas = model.GetObjects<TSqlSchema>(DacQueryScopes.Default);
            StringBuilder outputstring = new StringBuilder();

            foreach(var schema in schemas)
            {
                if (schema.Name.Parts[0] == "dbo")
                { 
                    outputstring.AppendFormat("\npackage {0} {{", schema.Name);

                    // put tables here
                    var x = schema.GetChildren(DacQueryScopes.UserDefined);


                    foreach (var thing in x)
                    {
                        if (thing.ObjectType == ModelSchema.Table)
                        {
                            var tbl = new TSqlTable(thing);
                            outputstring.Append("\n");
                            outputstring.Append(OutputDiagramTableDef(tbl,outputFormat));
                            outputstring.Append("\n");
                        }
                    }

                    outputstring.Append("}\n");
                }
            }

            schemas = model.GetObjects<TSqlSchema>(DacQueryScopes.UserDefined);
            foreach (var schema in schemas)
            {
                    outputstring.AppendFormat("\npackage {0} {{", schema.Name);

                    // put tables here
                    var x = schema.GetChildren(DacQueryScopes.UserDefined);
                    

                    foreach (var thing in x)
                    {
                        if (thing.ObjectType == ModelSchema.Table)
                        {
                            var tbl = new TSqlTable(thing);
                            outputstring.Append("\n");
                            outputstring.Append(OutputDiagramTableDef(tbl,outputFormat));
                            outputstring.Append("\n");
                        }
                   }

                    outputstring.Append("\n}}");

            }


            return (outputstring.ToString());

        }

        //output a single table in plantUML format
        private static string OutputDiagramTableDef(TSqlTable tbl, string outputFormat)
        {
               StringBuilder outputString = new StringBuilder();

                // need to string [] from the name
                outputString.AppendFormat("table({0}) {{\n", removeQualifiers(tbl.Name.ToString()));

                OutputDiagramPrimaryKey(outputString, tbl,outputFormat);
                OutputDiagramForeignKey(outputString, tbl,outputFormat);
                OutputDiagramColumns(outputString, tbl,outputFormat);

                outputString.AppendLine(@"}");
                outputString.AppendLine("");
             
            return (outputString.ToString());
        }


 

        private static string OutputDiagramColumns(StringBuilder outputString, TSqlTable t, string outputFormat)
        {
            foreach (var Column in t.Columns)
            {

                outputString.AppendFormat("\t{0}:", Column.Name.Parts[2]);
                foreach (var columnDataType in Column.DataType)
                {
                    outputString.AppendFormat(" {0}\n", removeQualifiers(columnDataType.Name.ToString()));
                }
            }
            return (outputString.ToString());
        }

        private static string OutputDiagramPrimaryKey(StringBuilder outputString, TSqlTable t, string outputFormat)
        {
            foreach (var primaryKey in t.PrimaryKeyConstraints)
            {
                foreach (var primaryKeyColumn in primaryKey.Columns)
                {
                    outputString.AppendFormat("\t{0}:", primaryKeyColumn.Name.Parts[2]);

                    foreach (var primarykeyDataType in primaryKeyColumn.DataType)
                    {
                        outputString.AppendFormat(" {0} ", removeQualifiers(primarykeyDataType.Name.ToString()));
                    }

                    outputString.AppendFormat("<<PK>>\n");
                }
            }
            return (outputString.ToString());
        }

        private static string OutputDiagramForeignKey(StringBuilder outputString, TSqlTable t,string outputFormat)
        {
            foreach (var foreignKey in t.ForeignKeyConstraints)
            {
                foreach (var foreignKeyColumn in foreignKey.Columns)
                {
                    outputString.AppendFormat("\t{0}:", foreignKeyColumn.Name.Parts[2]);

                    foreach (var foreignKeyDataType in foreignKeyColumn.DataType)
                    {
                        outputString.AppendFormat(" {0} ", removeQualifiers(foreignKeyDataType.Name.ToString()));
                    }

                    outputString.AppendFormat("<<FK>>\n");
                }

            }
            return (outputString.ToString());
        }

        private static void OutputDiagramRelationships(TSqlTypedModel model,string outputFormat)
        {
            var rels = model.GetObjects<TSqlForeignKeyConstraint>(DacQueryScopes.UserDefined);

            foreach(var rel in rels)
            {
                System.Console.Write("{0}", removeQualifiers(rel.GetParent().Name.ToString()));

                foreach (var ft in rel.ForeignTable)
                {
                    System.Console.WriteLine(" -|> {0}:FK", removeQualifiers(ft.Name.ToString()));
                }
           

                

            }
  
        }

        
        private static async void GraphVizTest(TSqlTypedModel model)
        {

            string graphVizBin = @"C:\Program Files (x86)\Graphviz2.38\bin";
        
            Graph graph = Graph.Undirected
                 .Add(EdgeStatement.For("a", "b"))
                .Add(EdgeStatement.For("a", "c"));

          

            IRenderer renderer = new Renderer(graphVizBin);
            using (Stream file = File.Create("graph.png"))
            {
                await renderer.RunAsync(
                    graph, file,
                    RendererLayouts.Dot,
                    RendererFormats.Png,
                    CancellationToken.None);
            }

        }
    }

}

