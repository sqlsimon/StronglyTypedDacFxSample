using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Microsoft.SqlServer.Dac.Extensions.Prototype;
using Microsoft.SqlServer.Dac.Model;


namespace StronglyTypedDacFxSample
{
    class Program
    {
        static void Main(string[] args)
        {

            var model = new TSqlTypedModel(@"..\..\..\SampleDacPac\pubs.dacpac");

            //var s = docdbtest(model);

            var s = OutputPlantUmlSchemaDef(model);

            System.Console.WriteLine("///////////////////////////////////////////////////////////////////////////////////");

            System.Console.Write(s);
            System.Console.WriteLine("");
            OutputPlantUMLRelationships(model);

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
        private static string OutputPlantUmlSchemaDef(TSqlTypedModel model)
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
                            outputstring.Append(OutputPlantUMLTableDef(tbl));
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
                            outputstring.Append(OutputPlantUMLTableDef(tbl));
                            outputstring.Append("\n");
                        }
                   }

                    outputstring.Append("\n}}");

            }


            return (outputstring.ToString());

        }

        //output a single table in plantUML format
        private static string OutputPlantUMLTableDef(TSqlTable tbl)
        {
               StringBuilder outputString = new StringBuilder();

                // need to string [] from the name
                outputString.AppendFormat("table({0}) {{\n", removeQualifiers(tbl.Name.ToString()));

                OutputPlantUMLPrimaryKey(outputString, tbl);
                OutputPlantUMLForeignKey(outputString, tbl);
                OutputPlantUMLColumns(outputString, tbl);

                outputString.AppendLine(@"}");
                outputString.AppendLine("");
             
            return (outputString.ToString());
        }


 

        private static string OutputPlantUMLColumns(StringBuilder outputString, TSqlTable t)
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

        private static string OutputPlantUMLPrimaryKey(StringBuilder outputString, TSqlTable t)
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

        private static string OutputPlantUMLForeignKey(StringBuilder outputString, TSqlTable t)
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

        private static void OutputPlantUMLRelationships(TSqlTypedModel model)
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

    }

}

