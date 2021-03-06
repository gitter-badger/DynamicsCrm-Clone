﻿// this file was generated by the xRM Test Framework VS Extension

#region Imports

using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using LinkDev.Libraries.Common;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;

#endregion

namespace LinkDev.Clone.Plugins
{
	/// <summary>
	///     Takes a template and record information and copies it into a new record.<br />
	///     Version: 0.1.1
	/// </summary>
	public class CloneRecordByTemplate : CodeActivity
	{
		#region Arguments

		////[RequiredArgument]
		////[Input("Bool input"), Default("True")]
		////public InArgument<bool> Bool { get; set; }

		////[RequiredArgument]
		////[Input("DateTime input")]
		////[Default("2013-07-09T02:54:00Z")]
		////public InArgument<DateTime> DateTime { get; set; }

		////[RequiredArgument]
		////[Input("Decimal input")]
		////[Default("20.75")]
		////public InArgument<decimal> Decimal { get; set; }

		////[RequiredArgument]
		////[Input("Double input")]
		////[Default("200.2")]
		////public InArgument<double> Double { get; set; }

		////[RequiredArgument]
		////[Input("Int input")]
		////[Default("2322")]
		////public InArgument<int> Int { get; set; }

		////[RequiredArgument]
		////[Input("Money input")]
		////[Default("232.3")]
		////public InArgument<Money> Money { get; set; }

		////[RequiredArgument]
		////[Input("OptionSetValue input")]
		////[AttributeTarget("account", "industrycode")]
		////[Default("3")]
		////public InArgument<OptionSetValue> OptionSetValue { get; set; }

		[RequiredArgument]
		[Input("Record Entity Name")]
		public InArgument<string> RecordEntityNameArg { get; set; }

		[RequiredArgument]
		[Input("Record ID")]
		public InArgument<string> RecordIdArg { get; set; }

		[RequiredArgument]
		[Input("Clone Template")]
		[ReferenceTarget(CloneRecordTemplate.EntityLogicalName)]
		public InArgument<EntityReference> CloneTemplateArg { get; set; }

		[Output("Cloned Record ID")]
		public OutArgument<string> ClonedRecordIdArg { get; set; }

		#endregion

		protected override void Execute(CodeActivityContext context)
		{
			new CloneRecordByTemplateLogic().Execute(this, context, PluginUser.InitiatingUser);
		}
	}

	internal class CloneRecordByTemplateLogic : StepLogic<CloneRecordByTemplate>
	{
		protected override void ExecuteLogic()
		{
			codeActivity.CloneTemplateArg.Require(nameof(codeActivity.CloneTemplateArg));

			var templateRef = executionContext.GetValue(codeActivity.CloneTemplateArg);
			var recordEntityName = executionContext.GetValue(codeActivity.RecordEntityNameArg);
			var recordId = executionContext.GetValue(codeActivity.RecordIdArg);

			recordEntityName.RequireNotEmpty(nameof(recordEntityName));
			recordId.RequireNotEmpty(nameof(recordId));

			log.Log($"Retrieving the template record '{templateRef.Id}' ...");
			var template =
				service.Retrieve(templateRef.LogicalName, templateRef.Id, new ColumnSet(true)).ToEntity<CloneRecordTemplate>();
			log.Log($"Retrieved the template record '{templateRef.Id}':'{template.Name}'.");
			CrmHelpers.LogAttributeValues(template.Attributes, template, log);

			Entity record = null;
			string[] fields = null;

			if (template.Fields.IsNotEmpty())
			{
				fields = template.Fields.Split(',');
				log.Log($"Retrieving the record '{recordEntityName}':'{recordId}' ...");
				record = service.Retrieve(recordEntityName, Guid.Parse(recordId), new ColumnSet(fields));
				log.Log($"Retrieved the record '{recordEntityName}':'{recordId}'.");
				CrmHelpers.LogAttributeValues(record.Attributes, record, log);
			}

			var clonedRecord = new Entity(recordEntityName);

			if (record != null && fields != null)
			{
				log.Log("Copying field values ...");
				var existingFields = fields.Where(f => record.Attributes.Contains(f)).ToArray();
				log.Log(new LogEntry("Field list ...", information: string.Join(",", existingFields)));
				clonedRecord.Attributes.AddRange(existingFields.ToDictionary(f => f, f => record[f]));
			}

			if (template.CloneFlagField.IsNotEmpty())
			{
				clonedRecord[template.CloneFlagField] = true;
				log.Log($"Set 'cloned' flag in field '{template.CloneFlagField}'.");
			}

			log.Log("Creating clone ...");
			var id = service.Create(clonedRecord);
			log.Log($"Created clone: '{id}'.");

			executionContext.SetValue(codeActivity.ClonedRecordIdArg, id.ToString());
		}
	}
}
