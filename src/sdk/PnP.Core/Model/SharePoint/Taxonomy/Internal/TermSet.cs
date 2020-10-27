﻿using PnP.Core.Services;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.Json;

namespace PnP.Core.Model.SharePoint
{
    //TODO: the delete uri needs be removed once the needed fix is deployed to Graph
    [GraphType(Uri = V, Delete = "termStore/groups/{Parent.GraphId}/sets/{GraphId}", LinqGet = "termStore/groups/{Parent.GraphId}/sets", Beta = true)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2243:Attribute string literals should parse correctly", Justification = "<Pending>")]
    internal partial class TermSet : BaseDataModel<ITermSet>, ITermSet
    {
        private const string baseUri = "termstore/sets";
        private const string V = baseUri + "/{GraphId}";

        #region Construction
        public TermSet()
        {
            // Handler to construct the Add request for this group
            AddApiCallHandler = async (keyValuePairs) =>
            {
                // Define the JSON body of the update request based on the actual changes
                dynamic localizedNames = new List<dynamic>();
                foreach (var localizedName in LocalizedNames)
                {
                    dynamic field = new ExpandoObject();
                    field.languageTag = localizedName.LanguageTag;
                    field.name = localizedName.Name;
                    localizedNames.Add(field);
                }

                dynamic body = new ExpandoObject();
                body.localizedNames = localizedNames;
                body.parentGroup = new
                {
                    id = Group.Id
                };

                if (IsPropertyAvailable(p => p.Description))
                {
                    body.description = Description;
                }

                // Serialize object to json
                var bodyContent = JsonSerializer.Serialize(body, typeof(ExpandoObject), new JsonSerializerOptions { WriteIndented = false });

                var apiCall = await ApiHelper.ParseApiRequestAsync(this, baseUri).ConfigureAwait(false);

                return new ApiCall(apiCall, ApiType.GraphBeta, bodyContent);
            };
        }
        #endregion

        #region Properties
        public string Id { get => GetValue<string>(); set => SetValue(value); }

        public ITermSetLocalizedNameCollection LocalizedNames { get => GetModelCollectionValue<ITermSetLocalizedNameCollection>(); }

        public string Description { get => GetValue<string>(); set => SetValue(value); }

        public DateTimeOffset CreatedDateTime { get => GetValue<DateTimeOffset>(); set => SetValue(value); }

        [GraphProperty("children", Get = "termstore/sets/{GraphId}/children", Beta = true)]
        public ITermCollection Terms { get => GetModelCollectionValue<ITermCollection>(); }

        [GraphProperty("parentGroup", Expandable = true)]
        public ITermGroup Group
        {
            get
            {
                // Since we quite often have the group already as part of the termset collection let's use that 
                if (Parent != null && Parent.Parent != null)
                {
                    InstantiateNavigationProperty();
                    SetValue(Parent.Parent as TermGroup);
                    return GetValue<ITermGroup>();
                }

                // Seems there was no group available, so process the loaded group and assign it
                return GetModelValue<ITermGroup>();
                //if (!NavigationPropertyInstantiated())
                //{
                //    var termGroup = new TermGroup
                //    {
                //        PnPContext = this.PnPContext,
                //        Parent = this,
                //    };
                //    SetValue(termGroup);
                //    InstantiateNavigationProperty();
                //}
                //return GetValue<ITermGroup>();
            }
            //set
            //{
            //    // Only set if there was no proper parent 
            //    if (Parent == null || Parent.Parent != null)
            //    {
            //        InstantiateNavigationProperty();
            //        SetValue(value);
            //    }
            //}
        }

        public ITermSetPropertyCollection Properties { get => GetModelCollectionValue<ITermSetPropertyCollection>(); }

        [GraphProperty("relations", Get = "termstore/sets/{GraphId}/relations?$expand=fromTerm,set,toTerm", Beta = true)]
        public ITermRelationCollection Relations { get => GetModelCollectionValue<ITermRelationCollection>(); }

        [KeyProperty(nameof(Id))]
        public override object Key { get => Id; set => Id = value.ToString(); }
        #endregion

        #region Methods
        public void AddProperty(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var property = Properties.FirstOrDefault(p => p.KeyField == key);
            if (property != null)
            {
                // update
                property.Value = value;
            }
            else
            {
                // add
                (Properties as TermSetPropertyCollection).Add(new TermSetProperty() { KeyField = key, Value = value });
            }
        }
        #endregion
    }
}
