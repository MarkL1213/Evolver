using EvolverCore.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolverCore.Views.Components
{
    internal class ComponentGraphNode
    {
        internal ComponentGraphNode(ChartComponentBase chartComponent)
        { _chartComponent = chartComponent; }
        internal List<ComponentGraphNode> Parents { get; } = new List<ComponentGraphNode>();
        internal List<ComponentGraphNode> Children { get; } = new List<ComponentGraphNode>();

        ChartComponentBase? _chartComponent;

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

        ComponentGraphNode? _root;

        public void AddComponent(ChartComponentBase chartComponent)
        {
            ComponentGraphNode newNode = new ComponentGraphNode(chartComponent);
            if(_root == null)_root = newNode;

            //....
        }


        //maintain a graph of all components
        //handle syncing current bar and creating empty points in output plots
        //call Calculate(int currentBar) in dependency order for components that need history


        //handle switching to live for components that are getting live data
        //call Calculate(int currentBar) in dependency order as new data comes in
    }
}
