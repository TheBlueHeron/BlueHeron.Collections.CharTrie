// This file is used by Code Analysis to maintain SuppressMessage attributes that are applied to this project.
// Project-level suppressions either have no target or are given a specific target and scoped to a namespace, type, member, etc.
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Style", "IDE0130:Namespace does not match folder structure", Justification = "Group files by category", Scope = "namespace", Target = "~N:BlueHeron.Collections")]
[assembly: SuppressMessage("Style", "IDE0130:Namespace does not match folder structure", Justification = "Group files by category", Scope = "namespace", Target = "~N:BlueHeron.Collections.Trie")]
[assembly: SuppressMessage("Style", "IDE0130:Namespace does not match folder structure", Justification = "Group files by category", Scope = "namespace", Target = "~N:BlueHeron.Collections.Trie.Search")]
[assembly: SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "Nomen est omen.", Scope = "type", Target = "~T:BlueHeron.Collections.Trie.Trie")]
[assembly: SuppressMessage("Naming", "CA1724:Type names should not match namespaces", Justification = "Nomen est omen.", Scope = "type", Target = "~T:BlueHeron.Collections.Trie.Trie")]
[assembly: SuppressMessage("Naming", "CA1812:Avoid uninstantiated internal classes", Justification = "Json converter is indeed instantiated.", Scope = "type", Target = "~T:BlueHeron.Collections.Trie.Serialization.NodeDeserializer")]
[assembly: SuppressMessage("Naming", "CA1812:Avoid uninstantiated internal classes", Justification = "Json converter is indeed instantiated.", Scope = "type", Target = "~T:BlueHeron.Collections.Trie.Serialization.NodeSerializer")]
[assembly: SuppressMessage("Naming", "CA1812:Avoid uninstantiated internal classes", Justification = "Json converter is indeed instantiated.", Scope = "type", Target = "~T:BlueHeron.Collections.Trie.Serialization.TrieConverter")]