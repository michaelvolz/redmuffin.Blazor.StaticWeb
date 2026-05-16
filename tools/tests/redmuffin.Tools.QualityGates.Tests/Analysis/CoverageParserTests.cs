namespace redmuffin.Tools.QualityGates.Tests.Analysis;

using redmuffin.Tools.QualityGates.Analysis;

public sealed class CoverageParserTests
{
    [Test]
    public async Task should_parse_single_covered_line_from_cobertura_xml()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage line-rate="1" branch-rate="1" version="1.9" timestamp="1234567890">
              <packages>
                <package>
                  <classes>
                    <class name="Foo" filename="MyFile.cs" line-rate="1" branch-rate="1">
                      <lines>
                        <line number="10" hits="5" branch="false"/>
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """;

        var result = Parse(xml);

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[("MyFile.cs", 10)]).IsEqualTo(5);
    }

    [Test]
    public async Task should_parse_multiple_lines_across_multiple_classes()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage line-rate="0.5" branch-rate="1" version="1.9">
              <packages>
                <package>
                  <classes>
                    <class name="Foo" filename="A.cs" line-rate="1" branch-rate="1">
                      <lines>
                        <line number="1" hits="3" branch="false"/>
                        <line number="2" hits="0" branch="false"/>
                      </lines>
                    </class>
                    <class name="Bar" filename="B.cs" line-rate="1" branch-rate="1">
                      <lines>
                        <line number="5" hits="7" branch="false"/>
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """;

        var result = Parse(xml);

        await Assert.That(result.Count).IsEqualTo(3);
        await Assert.That(result[("A.cs", 1)]).IsEqualTo(3);
        await Assert.That(result[("A.cs", 2)]).IsEqualTo(0);
        await Assert.That(result[("B.cs", 5)]).IsEqualTo(7);
    }

    [Test]
    public async Task should_treat_zero_hits_as_uncovered()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage line-rate="0" branch-rate="1" version="1.9">
              <packages>
                <package>
                  <classes>
                    <class name="Foo" filename="C.cs" line-rate="0" branch-rate="1">
                      <lines>
                        <line number="42" hits="0" branch="false"/>
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """;

        var result = Parse(xml);

        await Assert.That(result[("C.cs", 42)]).IsEqualTo(0);
    }

    [Test]
    public async Task should_handle_missing_hits_attribute_as_uncovered()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage line-rate="0" branch-rate="1" version="1.9">
              <packages>
                <package>
                  <classes>
                    <class name="Foo" filename="D.cs" line-rate="0" branch-rate="1">
                      <lines>
                        <line number="1" branch="false"/>
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """;

        var result = Parse(xml);

        await Assert.That(result[("D.cs", 1)]).IsEqualTo(0);
    }

    [Test]
    public async Task should_return_empty_dictionary_for_xml_with_no_line_elements()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage line-rate="0" branch-rate="1" version="1.9">
              <packages>
                <package>
                  <classes>
                    <class name="Foo" filename="E.cs" line-rate="0" branch-rate="1">
                      <lines/>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """;

        var result = Parse(xml);

        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task should_return_empty_dictionary_for_different_class_filename_path_formats()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage line-rate="1" branch-rate="1" version="1.9">
              <packages>
                <package>
                  <classes>
                    <class name="Foo.Bar+Baz" filename="/home/flynn/src/Foo/Bar.cs" line-rate="1" branch-rate="1">
                      <lines>
                        <line number="99" hits="1" branch="false"/>
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """;

        var result = Parse(xml);

        await Assert.That(result[("/home/flynn/src/Foo/Bar.cs", 99)]).IsEqualTo(1);
    }

    [Test]
    public void should_throw_when_xml_is_malformed()
    {
        var xml = "this is not valid <coverage>";

        Assert.Throws<System.Xml.XmlException>(() => Parse(xml));
    }

    [Test]
    public void should_throw_when_file_does_not_exist()
    {
        Assert.Throws<DirectoryNotFoundException>(() => CoverageParser.Parse("/nonexistent/path/coverage.xml"));
    }

    /// <summary>Writes XML to a temp file and parses it.</summary>
    private static IDictionary<(string FilePath, int LineNumber), int> Parse(string xml)
    {
        var file = Path.Combine(Path.GetTempPath(), $"coverage_{Guid.NewGuid():N}.xml");
        File.WriteAllText(file, xml);
        return CoverageParser.Parse(file);
    }

    [Test]
    public async Task TryParseLineNumber_should_return_zero_when_input_is_null()
    {
        var ok = CoverageParser.TryParseLineNumber(null, out var number);
        await Assert.That(ok).IsFalse();
        await Assert.That(number).IsEqualTo(0);
    }
}
