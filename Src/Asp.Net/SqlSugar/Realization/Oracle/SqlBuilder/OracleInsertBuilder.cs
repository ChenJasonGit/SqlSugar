﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlSugar
{
    public class OracleInsertBuilder : InsertBuilder
    {
        public override string SqlTemplate
        {
            get
            {
                    return @"INSERT INTO {0} 
           ({1})
     VALUES
           ({2}) ;";

            }
        }
        public override string ToSqlString()
        {
            var identities = this.EntityInfo.Columns.Where(it => it.OracleSequenceName.IsValuable()).ToList();
            if (IsNoInsertNull)
            {
                DbColumnInfoList = DbColumnInfoList.Where(it => it.Value != null).ToList();
            }
            var groupList = DbColumnInfoList.GroupBy(it => it.TableId).ToList();
            var isSingle = groupList.Count() == 1;
            string columnsString = string.Join(UtilConstants.Dot, groupList.First().Select(it => Builder.GetTranslationColumnName(it.DbColumnName)));
            if (isSingle)
            {
                string columnParametersString = string.Join(UtilConstants.Dot, this.DbColumnInfoList.Select(it => Builder.SqlParameterKeyWord + it.DbColumnName));
                if (identities.IsValuable()) {
                    columnsString = columnsString.TrimEnd(UtilConstants.DotChar) + UtilConstants.Dot + string.Join(UtilConstants.Dot, identities.Select(it=> Builder.GetTranslationColumnName(it.DbColumnName)));
                    columnParametersString = columnParametersString.TrimEnd(UtilConstants.DotChar) + UtilConstants.Dot + string.Join(UtilConstants.Dot, identities.Select(it =>it.OracleSequenceName));
                }
                return string.Format(SqlTemplate, GetTableNameString, columnsString, columnParametersString);
            }
            else
            {
                StringBuilder batchInsetrSql = new StringBuilder();
                int pageSize = 200;
                int pageIndex = 1;
                int totalRecord = groupList.Count;
                int pageCount = (totalRecord + pageSize - 1) / pageSize;
                while (pageCount >= pageIndex)
                {
                    batchInsetrSql.AppendFormat(SqlTemplateBatch, GetTableNameString, columnsString);
                    int i = 0;
                    foreach (var columns in groupList.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList())
                    {
                        var isFirst = i == 0;
                        if (!isFirst)
                        {
                            batchInsetrSql.Append(SqlTemplateBatchUnion);
                        }
                        var insertColumns = string.Join(",", columns.Select(it => string.Format(SqlTemplateBatchSelect, FormatValue(it.Value), Builder.GetTranslationColumnName(it.DbColumnName))));
                        batchInsetrSql.Append("\r\n SELECT " + insertColumns + " FROM DUAL ");
                        ++i;
                    }
                    pageIndex++;
                    batchInsetrSql.Append("\r\n;\r\n");
                }
                return batchInsetrSql.ToString();
            }
        }
    }
}
