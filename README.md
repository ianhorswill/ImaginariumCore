*ImaginariumCore* is the basic parser, solver, and NL generator for the *Imaginarium*
constraint-based procedural content generation system, packaged as a DLL for use in
any Unity project.  To use it, add the following files to your Assets directory:

* The compiled `Imaginarium.dll` file from the enclosed Imaginarium project
* The `CatSAT.dll` file here, or some other copy
* The `Inflections` folder and its contents, form the Imaginarium project

Then add the following to your code:

* Set `Imaginarium.Driver.DataFiles` to the path of the folder holding the `Inflections` folder.
* Call `new Imaginarium.Ontology.Ontology(generatorPath)`, where `generatorPath`
  is a path to the directory holding your generator definition (you can create and debug the 
  generators using the Imaginarium IDE)
* Call `o.CommonNoun("noun").MakeGenerator().Generate()` where `o` is the ontology, and
  `"noun"` is the name of the kind of thing to generate, e.g. "character", "monster", "cat", etc.
* The result is an object of type `Invention`.  It has a field called PossibleIndividuals, which is
  an array of the created objects.  If you just asked for one object, it will be `i.PossibleIndividuals[0]`,
  where `i` is the invention you generated.
* The PossibleIndividual class has the following useful members
   * `NameString()` returns the whatever name has been assigned to the individual.
   * `Description()` generates an English description of the individual
   * `IsA(concept)` tells you if the concept (a common noun or adjective) holds of
      the individual.
   * `RelatesTo(that, verb)` tells you if this possible individual verbs that.