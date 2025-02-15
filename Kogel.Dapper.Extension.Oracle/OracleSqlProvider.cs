﻿using System;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Kogel.Dapper.Extension.Core.Interfaces;
using Kogel.Dapper.Extension.Oracle.Extension;

namespace Kogel.Dapper.Extension.Oracle
{
    public class OracleSqlProvider : SqlProvider
    {
        private const string OpenQuote = "\"";
        private const string CloseQuote = "\"";
        private const char ParameterPrefix = ':';
        private IResolveExpression ResolveExpression;
        public OracleSqlProvider()
        {
            ProviderOption = new ProviderOption(OpenQuote, CloseQuote, ParameterPrefix);
            ResolveExpression = new ResolveExpression(ProviderOption);
        }

        public sealed override IProviderOption ProviderOption { get; set; }

        public override SqlProvider FormatGet<T>()
        {
            var selectSql = ResolveExpression.ResolveSelect(EntityCache.QueryEntity(typeof(T)), Context.Set.SelectExpression, null, Params);

            var fromTableSql = FormatTableName();

            var joinSql = ResolveExpression.ResolveJoinSql(JoinList, ref selectSql, Context.Set.SelectExpression);

            var whereSql = string.Empty;

            //表查询条件
            var whereParamsList = ResolveExpression.ResolveWhereList(Context.Set,ref whereSql, Params);

            var orderbySql = ResolveExpression.ResolveOrderBy(Context.Set);

            SqlString = $@"SELECT T.* FROM( 
                            {selectSql}
                            {fromTableSql} {joinSql}
                            {whereSql}
                            {orderbySql}
                            ) T
                            WHERE ROWNUM<=1";

            return this;
        }

        public override SqlProvider FormatToList<T>()
        {
            var topNum = DataBaseContext<T>().QuerySet.TopNum;

            var selectSql = ResolveExpression.ResolveSelect(EntityCache.QueryEntity(typeof(T)), Context.Set.SelectExpression, null, Params);

            var fromTableSql = FormatTableName();

            var joinSql = ResolveExpression.ResolveJoinSql(JoinList, ref selectSql, Context.Set.SelectExpression);

            var whereSql = string.Empty;

            //表查询条件
            var whereParamsList = ResolveExpression.ResolveWhereList(Context.Set, ref whereSql, Params);

            var orderbySql = ResolveExpression.ResolveOrderBy(Context.Set);

            SqlString = $"{selectSql} {fromTableSql} {joinSql} {whereSql} {orderbySql}";

            return this;
        }

        public override SqlProvider FormatToPageList<T>(int pageIndex, int pageSize,bool IsSelectCount=true)
        {
            var orderbySql = ResolveExpression.ResolveOrderBy(Context.Set);
            //Oracle可以不用必须排序翻页
            //if (string.IsNullOrEmpty(orderbySql))
            //    throw new DapperExtensionException("order by takes precedence over pagelist");

            var selectSql = ResolveExpression.ResolveSelect(EntityCache.QueryEntity(typeof(T)), Context.Set.SelectExpression, null, Params);

            var fromTableSql = FormatTableName();

            var joinSql = ResolveExpression.ResolveJoinSql(JoinList, ref selectSql, Context.Set.SelectExpression);

            var whereSql = string.Empty;

            //表查询条件
            var whereParamsList = ResolveExpression.ResolveWhereList(Context.Set, ref whereSql, Params);

            SqlString = $@" SELECT T2.* FROM(
                            SELECT T.*,ROWNUM ROWNUMS FROM (
                            SELECT 
                            {(new Regex("SELECT").Replace(selectSql, "", 1))}
                            {fromTableSql} {joinSql} {whereSql} {orderbySql}
                            ) T 
                            )T2
                            WHERE ROWNUMS BETWEEN {((pageIndex - 1) * pageSize) + 1} and {pageIndex * pageSize}";

            IsSelectCount = true;
            return this;
        }

        public override SqlProvider FormatCount()
        {
            var selectSql = "SELECT COUNT(1)";

            var fromTableSql = FormatTableName();

            string noneSql = "";
            var joinSql = ResolveExpression.ResolveJoinSql(JoinList, ref noneSql, Context.Set.SelectExpression);

            var whereSql = string.Empty;

            //表查询条件
            var whereParamsList = ResolveExpression.ResolveWhereList(Context.Set, ref whereSql, Params);

            SqlString = $"{selectSql} {fromTableSql} {joinSql} {whereSql} ";

            return this;
        }

        public override SqlProvider FormatDelete()
        {
            var fromTableSql = ProviderOption.CombineFieldName(FormatTableName(false, false).Trim());
            var whereSql = string.Empty;
            //表查询条件
            var whereParamsList = ResolveExpression.ResolveWhereList(Context.Set, ref whereSql, Params, null, false);
            SqlString = $"DELETE {fromTableSql} {whereSql}";
            return this;
        }

        public override SqlProvider FormatInsert<T>(T entity, string[] excludeFields)
		{
            var fromTableSql = ProviderOption.CombineFieldName(FormatTableName(false, false).Trim());
            var paramsAndValuesSql = FormatInsertParamsAndValues(entity);
            SqlString = $"INSERT INTO {fromTableSql} ({paramsAndValuesSql[0]}) VALUES({paramsAndValuesSql[1]})";
            return this;
        }

        public override SqlProvider FormatInsertIdentity<T>(T entity, string[] excludeFields)
		{
            var fromTableSql = ProviderOption.CombineFieldName(FormatTableName(false, false).Trim());
            var paramsAndValuesSql = FormatInsertParamsAndValues(entity);
            SqlString = $"INSERT INTO {fromTableSql} ({paramsAndValuesSql[0]}) VALUES({paramsAndValuesSql[1]}) SELECT @@IDENTITY";
            return this;
        }

        public override SqlProvider FormatUpdate<T>(Expression<Func<T, T>> updateExpression)
        {
            var update = ResolveExpression.ResolveUpdate(updateExpression);

            var fromTableSql = ProviderOption.CombineFieldName(FormatTableName(false, false).Trim());

            var whereSql = string.Empty;

            //表查询条件
            var whereParamsList = ResolveExpression.ResolveWhereList(Context.Set, ref whereSql, Params, null, false);
            Params.AddDynamicParams(update.Param);

            SqlString = $"UPDATE {fromTableSql} {update.SqlCmd} {whereSql}";

            return this;
        }

        public override SqlProvider FormatUpdate<T>(T entity, string[] excludeFields)
        {
			var update = ResolveExpression.ResolveUpdates<T>(entity, Params, excludeFields);
            var fromTableSql = ProviderOption.CombineFieldName(FormatTableName(false, false).Trim());
            var whereSql = string.Empty;
            //表查询条件
            var whereParamsList = ResolveExpression.ResolveWhereList(Context.Set, ref whereSql, Params, null, false);

            SqlString = $"UPDATE {fromTableSql} {update} {whereSql}";
            return this;
        }

        public override SqlProvider FormatSum(LambdaExpression sumExpression)
        {
            var selectSql = ResolveExpression.ResolveSum(sumExpression);

            var fromTableSql = FormatTableName();

            string noneSql = "";
            var joinSql = ResolveExpression.ResolveJoinSql(JoinList, ref noneSql, Context.Set.SelectExpression);

            var whereSql = string.Empty;

            //表查询条件
            var whereParamsList = ResolveExpression.ResolveWhereList(Context.Set, ref whereSql, Params);

            SqlString = $"{selectSql} {fromTableSql}{joinSql} {whereSql} ";

            return this;
        }

        public override SqlProvider FormatMin(LambdaExpression minExpression)
        {
            var selectSql = ResolveExpression.ResolveMin(minExpression);

            var fromTableSql = FormatTableName();

            string noneSql = "";
            var joinSql = ResolveExpression.ResolveJoinSql(JoinList, ref noneSql, Context.Set.SelectExpression);

            var whereSql = string.Empty;

            //表查询条件
            var whereParamsList = ResolveExpression.ResolveWhereList(Context.Set, ref whereSql, Params);

            SqlString = $"{selectSql} {fromTableSql}{joinSql} {whereSql} ";

            return this;
        }
        public override SqlProvider FormatMax(LambdaExpression maxExpression)
        {
            var selectSql = ResolveExpression.ResolveMax(maxExpression);

            var fromTableSql = FormatTableName();

            string noneSql = "";
            var joinSql = ResolveExpression.ResolveJoinSql(JoinList, ref noneSql, Context.Set.SelectExpression);

            var whereSql = string.Empty;

            //表查询条件
            var whereParamsList = ResolveExpression.ResolveWhereList(Context.Set, ref whereSql, Params);

            SqlString = $"{selectSql} {fromTableSql}{joinSql} {whereSql} ";

            return this;
        }


        public override SqlProvider FormatUpdateSelect<T>(Expression<Func<T, T>> updator)
        {
            var update = ResolveExpression.ResolveUpdate(updator);

            var fromTableSql = ProviderOption.CombineFieldName(FormatTableName(false, false).Trim());
            var selectSql = ResolveExpression.ResolveSelectOfUpdate(EntityCache.QueryEntity(typeof(T)), Context.Set.SelectExpression);

            var whereSql = string.Empty;

            //表查询条件
            var whereParamsList = ResolveExpression.ResolveWhereList(Context.Set, ref whereSql, Params, null, false);
            Params.AddDynamicParams(update.Param);

            SqlString = $"UPDATE {fromTableSql} {update.SqlCmd} {selectSql} {whereSql}";

            return this;
        }
    }
}
