using EvolverCore.ViewModels;
using EvolverCore.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace EvolverCore.Views.Components
{
    internal abstract class TreeNode
    {
        internal List<ComponentTreeNode> Children { get; } = new List<ComponentTreeNode>();
        
    }

    internal class ComponentTreeRootNode : TreeNode
    {
        internal ComponentTreeRootNode(BarDataSeries data, ChartComponentBase? component = null)
        {
            Data = data;
            if (component != null)
            {
                ComponentTreeNode newNode = new ComponentTreeNode(component);
                newNode.Parents.Add(this);
                Children.Add(newNode);
            }
        }
        //internal List<ChartComponentBase> DataComponents { get; } = new List<ChartComponentBase>();

        internal BarDataSeries? Data { set; get; }

        internal ComponentTreeNode? FindComponent(ChartComponentBase chartComponent, bool searchChildren = true)
        {
            foreach (ComponentTreeNode child in Children)
            {
                ComponentTreeNode? x = child.FindComponent(chartComponent, searchChildren);
                if (x != null) return x;
            }

            return null;
        }

    }


    internal class ComponentTreeNode : TreeNode
    {
        internal ComponentTreeNode(ChartComponentBase chartComponent)
        { Node = chartComponent; }
        internal List<TreeNode> Parents { get; } = new List<TreeNode>();
        internal ChartComponentBase? Node { set; get; }

        internal ComponentTreeNode? FindComponent(ChartComponentBase chartComponent, bool searchChildren = true)
        {
            if (Node == chartComponent) return this;
            if (searchChildren)
            {
                foreach (ComponentTreeNode child in Children)
                {
                    ComponentTreeNode? x = child.FindComponent(chartComponent, searchChildren);
                    if (x != null) return x;
                }
            }
            return null;
        }


    }


    internal class ChartComponentController
    {
        private static readonly ChartComponentController _instance = new ChartComponentController();
        public static ChartComponentController Instance { get { return _instance; } }
        static ChartComponentController()
        {
        }

        private ChartComponentController()
        {
            
        }

        List<ComponentTreeRootNode> _rootList = new List<ComponentTreeRootNode>();

        public TreeNode? FindOrAddComponent(ChartComponentBase chartComponent)
        {
            ChartComponentViewModel componentVM = chartComponent.Properties;
            TreeNode? node;
            if (chartComponent is Data)
            {
                if (componentVM.Data == null) return null;

                TreeNode? rootNode;
                rootNode = FindData(componentVM.Data, chartComponent as Data);
                if (rootNode == null)
                {
                    rootNode = FindOrAddData(componentVM.Data, chartComponent as Data);
                }

                return rootNode;
            }

            node = FindComponent(chartComponent);
            if (node != null) { return node; }

            ComponentTreeNode newNode = new ComponentTreeNode(chartComponent);
            bool wired = false;

            if (componentVM.Data != null)
            {
                TreeNode? parentData = FindOrAddData(componentVM.Data);
                if (parentData != null)
                {
                    newNode.Parents.Add(parentData);
                    parentData.Children.Add(newNode);
                    wired = true;
                }
            }
            
            if (componentVM.SourceIndicator != null)
            {

                //TreeNode? parentSource = FindOrAddComponent(componentVM.SourceIndicator);
                //if (parentSource != null)
                //{
                //    newNode.Parents.Add(parentSource);
                //    parentSource.Children.Add(newNode);
                //    wired = true;
                //}
            }

            return wired ? newNode : null;
        }

        TreeNode? FindData(BarDataSeries dataSereis, Data? component=null)
        {
            foreach (ComponentTreeRootNode dataNode in _rootList)
                if (dataNode.Data != null && dataNode.Data == dataSereis)
                {
                    if (component != null)
                    {
                        foreach (ComponentTreeNode child in dataNode.Children)
                        {
                            if (child.Node == component) return child;
                        }
                    }
                    else
                        return dataNode;
                }
            return null;
        }

        TreeNode? FindOrAddData(BarDataSeries dataSeries, Data? component=null)
        {
            TreeNode? findResult = FindData(dataSeries, component);
            if (findResult != null && findResult is ComponentTreeNode) { return findResult; }

            if (findResult != null && findResult is ComponentTreeRootNode )
            {
                if (component != null)
                {
                    ComponentTreeNode newNode = new ComponentTreeNode(component);
                    newNode.Parents.Add(findResult);
                    findResult.Children.Add(newNode);
                    return newNode;
                }
                else
                {
                    return findResult;
                }
            }

            if (findResult == null)
            {
                ComponentTreeRootNode resultNode = new ComponentTreeRootNode(dataSeries, component);
                _rootList.Add(resultNode);
                return component != null ? resultNode.Children[0]  : resultNode;
            }

            return null;
        }

        TreeNode? FindComponent(ChartComponentBase chartComponent, bool searchChildren = true)
        {
            foreach (ComponentTreeRootNode rootNode in _rootList)
            {
                    foreach (ComponentTreeNode child in rootNode.Children)
                    {
                        ComponentTreeNode? r = child.FindComponent(chartComponent,searchChildren);
                        if (r != null) return r;
                    }
            }
 
           return null;
        }


        //maintain a graph of all components
        //handle syncing current bar and creating empty points in output plots
        //call Calculate(int currentBar) in dependency order for components that need history


        //handle switching to live for components that are getting live data
        //call Calculate(int currentBar) in dependency order as new data comes in
    }
}
