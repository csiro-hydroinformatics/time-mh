using NVelocity.App;
using Commons.Collections;
using NVelocity.Runtime;
namespace CSIRO.Metaheuristics.UseCases.PEST.FileCreation
{
    public abstract class PestFileCreator
    {
        public abstract void CreateFile();

        protected VelocityEngine createNewVelocityEngine()
        {
            VelocityEngine velocity = new VelocityEngine();
            var props = new ExtendedProperties();

            props.AddProperty(RuntimeConstants.RESOURCE_LOADER, "assembly");
            props.AddProperty("assembly.resource.loader.class",
                              "NVelocity.Runtime.Resource.Loader.AssemblyResourceLoader, NVelocity");
            props.AddProperty("assembly.resource.loader.assembly", GetType().Assembly.GetName().Name);
            velocity.Init(props);
            return velocity;
        }
    }

}
