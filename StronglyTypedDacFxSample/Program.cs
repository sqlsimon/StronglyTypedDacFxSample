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

            var s = OutputPlantUMLTableDef(model);

            System.Console.WriteLine("///////////////////////////////////////////////////////////////////////////////////");

            System.Console.Write(s);

            System.Console.ReadKey();



        }


        private static string removeQualifiers(string inputString)
        {
            return (inputString.Replace("[", "").Replace("]", ""));

        }

        private static string OutputPlantUMLTableDef(TSqlTypedModel model)
        {
            var tables = model.GetObjects<TSqlTable>(DacQueryScopes.UserDefined);
            StringBuilder outputString = new StringBuilder();

            foreach (var t in tables)
            {
                // need to string [] from the name
                outputString.AppendFormat("table({0}) {{\n", removeQualifiers(t.Name.ToString()));

                OutputPlantUMLPrimaryKey(outputString, t);
                OutputPlantUMLForeignKey(outputString, t);
                OutputPlantUMLColumns(outputString, t);

                outputString.AppendLine(@"}");
                outputString.AppendLine("");

                // NEED TO WRITE CODE THAT OUTPUTS RELATIONSHIPS
                //OutputPlantUMLRelationships(outputString, model);

            }

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
    }

}

