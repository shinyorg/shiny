using System;

namespace Sample;


public partial class TagsPage : SampleContentPage
{
    public TagsPage(TagsViewModel vm)
    {
        this.InitializeComponent();
        this.BindingContext = vm;
    }
}