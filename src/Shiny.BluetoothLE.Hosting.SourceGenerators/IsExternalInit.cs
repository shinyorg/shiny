using System.ComponentModel;

namespace System.Runtime.CompilerServices;


// netstandard2.0 has no IsExternalInit, and records/init setters need it
[EditorBrowsable(EditorBrowsableState.Never)]
static class IsExternalInit;
