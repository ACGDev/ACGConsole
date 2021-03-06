﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace AutoCarConsole.ACG_CK {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(Namespace="http://api.coverking.com/Orders", ConfigurationName="ACG_CK.Order_PlacementSoap")]
    public interface Order_PlacementSoap {
        
        // CODEGEN: Generating message contract since message HelloWorldRequest has headers
        [System.ServiceModel.OperationContractAttribute(Action="http://api.coverking.com/Orders/HelloWorld", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        AutoCarConsole.ACG_CK.HelloWorldResponse HelloWorld(AutoCarConsole.ACG_CK.HelloWorldRequest request);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://api.coverking.com/Orders/HelloWorld", ReplyAction="*")]
        System.Threading.Tasks.Task<AutoCarConsole.ACG_CK.HelloWorldResponse> HelloWorldAsync(AutoCarConsole.ACG_CK.HelloWorldRequest request);
        
        // CODEGEN: Generating message contract since message URL_FileRequest has headers
        [System.ServiceModel.OperationContractAttribute(Action="http://api.coverking.com/Orders/URL_File", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        AutoCarConsole.ACG_CK.URL_FileResponse URL_File(AutoCarConsole.ACG_CK.URL_FileRequest request);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://api.coverking.com/Orders/URL_File", ReplyAction="*")]
        System.Threading.Tasks.Task<AutoCarConsole.ACG_CK.URL_FileResponse> URL_FileAsync(AutoCarConsole.ACG_CK.URL_FileRequest request);
        
        // CODEGEN: Generating message contract since message Place_OrdersRequest has headers
        [System.ServiceModel.OperationContractAttribute(Action="http://api.coverking.com/Orders/Place_Orders", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        AutoCarConsole.ACG_CK.Place_OrdersResponse Place_Orders(AutoCarConsole.ACG_CK.Place_OrdersRequest request);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://api.coverking.com/Orders/Place_Orders", ReplyAction="*")]
        System.Threading.Tasks.Task<AutoCarConsole.ACG_CK.Place_OrdersResponse> Place_OrdersAsync(AutoCarConsole.ACG_CK.Place_OrdersRequest request);
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.6.1064.2")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://api.coverking.com/Orders")]
    public partial class Auth_Header : object, System.ComponentModel.INotifyPropertyChanged {
        
        private string dealerIDField;
        
        private string passwordField;
        
        private System.Xml.XmlAttribute[] anyAttrField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=0)]
        public string DealerID {
            get {
                return this.dealerIDField;
            }
            set {
                this.dealerIDField = value;
                this.RaisePropertyChanged("DealerID");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=1)]
        public string Password {
            get {
                return this.passwordField;
            }
            set {
                this.passwordField = value;
                this.RaisePropertyChanged("Password");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyAttributeAttribute()]
        public System.Xml.XmlAttribute[] AnyAttr {
            get {
                return this.anyAttrField;
            }
            set {
                this.anyAttrField = value;
                this.RaisePropertyChanged("AnyAttr");
            }
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.6.1064.2")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://api.coverking.com/Orders")]
    public partial class Place_Order_Response : object, System.ComponentModel.INotifyPropertyChanged {
        
        private bool processedField;
        
        private string[] response_messageField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=0)]
        public bool Processed {
            get {
                return this.processedField;
            }
            set {
                this.processedField = value;
                this.RaisePropertyChanged("Processed");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order=1)]
        public string[] Response_message {
            get {
                return this.response_messageField;
            }
            set {
                this.response_messageField = value;
                this.RaisePropertyChanged("Response_message");
            }
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.6.1064.2")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://api.coverking.com/Orders")]
    public partial class Order : object, System.ComponentModel.INotifyPropertyChanged {
        
        private string poField;
        
        private System.DateTime pO_DateField;
        
        private string ship_CompanyField;
        
        private string ship_NameField;
        
        private string ship_AddrField;
        
        private string ship_Addr_2Field;
        
        private string ship_CityField;
        
        private string ship_StateField;
        
        private string ship_ZipField;
        
        private string ship_CountryField;
        
        private string ship_PhoneField;
        
        private string ship_EmailField;
        
        private string ship_ServiceField;
        
        private string cK_SKUField;
        
        private string cK_ItemField;
        
        private string cK_VariantField;
        
        private string customized_CodeField;
        
        private string customized_MsgField;
        
        private string customized_Code2Field;
        
        private string customized_Msg2Field;
        
        private int qtyField;
        
        private string commentField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=0)]
        public string PO {
            get {
                return this.poField;
            }
            set {
                this.poField = value;
                this.RaisePropertyChanged("PO");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=1)]
        public System.DateTime PO_Date {
            get {
                return this.pO_DateField;
            }
            set {
                this.pO_DateField = value;
                this.RaisePropertyChanged("PO_Date");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=2)]
        public string Ship_Company {
            get {
                return this.ship_CompanyField;
            }
            set {
                this.ship_CompanyField = value;
                this.RaisePropertyChanged("Ship_Company");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=3)]
        public string Ship_Name {
            get {
                return this.ship_NameField;
            }
            set {
                this.ship_NameField = value;
                this.RaisePropertyChanged("Ship_Name");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=4)]
        public string Ship_Addr {
            get {
                return this.ship_AddrField;
            }
            set {
                this.ship_AddrField = value;
                this.RaisePropertyChanged("Ship_Addr");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=5)]
        public string Ship_Addr_2 {
            get {
                return this.ship_Addr_2Field;
            }
            set {
                this.ship_Addr_2Field = value;
                this.RaisePropertyChanged("Ship_Addr_2");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=6)]
        public string Ship_City {
            get {
                return this.ship_CityField;
            }
            set {
                this.ship_CityField = value;
                this.RaisePropertyChanged("Ship_City");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=7)]
        public string Ship_State {
            get {
                return this.ship_StateField;
            }
            set {
                this.ship_StateField = value;
                this.RaisePropertyChanged("Ship_State");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=8)]
        public string Ship_Zip {
            get {
                return this.ship_ZipField;
            }
            set {
                this.ship_ZipField = value;
                this.RaisePropertyChanged("Ship_Zip");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=9)]
        public string Ship_Country {
            get {
                return this.ship_CountryField;
            }
            set {
                this.ship_CountryField = value;
                this.RaisePropertyChanged("Ship_Country");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=10)]
        public string Ship_Phone {
            get {
                return this.ship_PhoneField;
            }
            set {
                this.ship_PhoneField = value;
                this.RaisePropertyChanged("Ship_Phone");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=11)]
        public string Ship_Email {
            get {
                return this.ship_EmailField;
            }
            set {
                this.ship_EmailField = value;
                this.RaisePropertyChanged("Ship_Email");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=12)]
        public string Ship_Service {
            get {
                return this.ship_ServiceField;
            }
            set {
                this.ship_ServiceField = value;
                this.RaisePropertyChanged("Ship_Service");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=13)]
        public string CK_SKU {
            get {
                return this.cK_SKUField;
            }
            set {
                this.cK_SKUField = value;
                this.RaisePropertyChanged("CK_SKU");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=14)]
        public string CK_Item {
            get {
                return this.cK_ItemField;
            }
            set {
                this.cK_ItemField = value;
                this.RaisePropertyChanged("CK_Item");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=15)]
        public string CK_Variant {
            get {
                return this.cK_VariantField;
            }
            set {
                this.cK_VariantField = value;
                this.RaisePropertyChanged("CK_Variant");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=16)]
        public string Customized_Code {
            get {
                return this.customized_CodeField;
            }
            set {
                this.customized_CodeField = value;
                this.RaisePropertyChanged("Customized_Code");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=17)]
        public string Customized_Msg {
            get {
                return this.customized_MsgField;
            }
            set {
                this.customized_MsgField = value;
                this.RaisePropertyChanged("Customized_Msg");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=18)]
        public string Customized_Code2 {
            get {
                return this.customized_Code2Field;
            }
            set {
                this.customized_Code2Field = value;
                this.RaisePropertyChanged("Customized_Code2");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=19)]
        public string Customized_Msg2 {
            get {
                return this.customized_Msg2Field;
            }
            set {
                this.customized_Msg2Field = value;
                this.RaisePropertyChanged("Customized_Msg2");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=20)]
        public int Qty {
            get {
                return this.qtyField;
            }
            set {
                this.qtyField = value;
                this.RaisePropertyChanged("Qty");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=21)]
        public string Comment {
            get {
                return this.commentField;
            }
            set {
                this.commentField = value;
                this.RaisePropertyChanged("Comment");
            }
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.6.1064.2")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://api.coverking.com/Orders")]
    public partial class File_Header : object, System.ComponentModel.INotifyPropertyChanged {
        
        private string fileNameField;
        
        private string fileContentTextField;
        
        private System.Xml.XmlAttribute[] anyAttrField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=0)]
        public string FileName {
            get {
                return this.fileNameField;
            }
            set {
                this.fileNameField = value;
                this.RaisePropertyChanged("FileName");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=1)]
        public string FileContentText {
            get {
                return this.fileContentTextField;
            }
            set {
                this.fileContentTextField = value;
                this.RaisePropertyChanged("FileContentText");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyAttributeAttribute()]
        public System.Xml.XmlAttribute[] AnyAttr {
            get {
                return this.anyAttrField;
            }
            set {
                this.anyAttrField = value;
                this.RaisePropertyChanged("AnyAttr");
            }
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="HelloWorld", WrapperNamespace="http://api.coverking.com/Orders", IsWrapped=true)]
    public partial class HelloWorldRequest {
        
        [System.ServiceModel.MessageHeaderAttribute(Namespace="http://api.coverking.com/Orders")]
        public AutoCarConsole.ACG_CK.Auth_Header Auth_Header;
        
        public HelloWorldRequest() {
        }
        
        public HelloWorldRequest(AutoCarConsole.ACG_CK.Auth_Header Auth_Header) {
            this.Auth_Header = Auth_Header;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="HelloWorldResponse", WrapperNamespace="http://api.coverking.com/Orders", IsWrapped=true)]
    public partial class HelloWorldResponse {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://api.coverking.com/Orders", Order=0)]
        public string HelloWorldResult;
        
        public HelloWorldResponse() {
        }
        
        public HelloWorldResponse(string HelloWorldResult) {
            this.HelloWorldResult = HelloWorldResult;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="URL_File", WrapperNamespace="http://api.coverking.com/Orders", IsWrapped=true)]
    public partial class URL_FileRequest {
        
        [System.ServiceModel.MessageHeaderAttribute(Namespace="http://api.coverking.com/Orders")]
        public AutoCarConsole.ACG_CK.Auth_Header Auth_Header;
        
        [System.ServiceModel.MessageHeaderAttribute(Namespace="http://api.coverking.com/Orders")]
        public AutoCarConsole.ACG_CK.File_Header File_Header;
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://api.coverking.com/Orders", Order=0)]
        public string URL;
        
        public URL_FileRequest() {
        }
        
        public URL_FileRequest(AutoCarConsole.ACG_CK.Auth_Header Auth_Header, AutoCarConsole.ACG_CK.File_Header File_Header, string URL) {
            this.Auth_Header = Auth_Header;
            this.File_Header = File_Header;
            this.URL = URL;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="URL_FileResponse", WrapperNamespace="http://api.coverking.com/Orders", IsWrapped=true)]
    public partial class URL_FileResponse {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://api.coverking.com/Orders", Order=0)]
        public string[] URL_FileResult;
        
        public URL_FileResponse() {
        }
        
        public URL_FileResponse(string[] URL_FileResult) {
            this.URL_FileResult = URL_FileResult;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="Place_Orders", WrapperNamespace="http://api.coverking.com/Orders", IsWrapped=true)]
    public partial class Place_OrdersRequest {
        
        [System.ServiceModel.MessageHeaderAttribute(Namespace="http://api.coverking.com/Orders")]
        public AutoCarConsole.ACG_CK.Auth_Header Auth_Header;
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://api.coverking.com/Orders", Order=0)]
        public AutoCarConsole.ACG_CK.Order[] Orders;
        
        public Place_OrdersRequest() {
        }
        
        public Place_OrdersRequest(AutoCarConsole.ACG_CK.Auth_Header Auth_Header, AutoCarConsole.ACG_CK.Order[] Orders) {
            this.Auth_Header = Auth_Header;
            this.Orders = Orders;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="Place_OrdersResponse", WrapperNamespace="http://api.coverking.com/Orders", IsWrapped=true)]
    public partial class Place_OrdersResponse {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://api.coverking.com/Orders", Order=0)]
        public AutoCarConsole.ACG_CK.Place_Order_Response Place_OrdersResult;
        
        public Place_OrdersResponse() {
        }
        
        public Place_OrdersResponse(AutoCarConsole.ACG_CK.Place_Order_Response Place_OrdersResult) {
            this.Place_OrdersResult = Place_OrdersResult;
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface Order_PlacementSoapChannel : AutoCarConsole.ACG_CK.Order_PlacementSoap, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class Order_PlacementSoapClient : System.ServiceModel.ClientBase<AutoCarConsole.ACG_CK.Order_PlacementSoap>, AutoCarConsole.ACG_CK.Order_PlacementSoap {
        
        public Order_PlacementSoapClient() {
        }
        
        public Order_PlacementSoapClient(string endpointConfigurationName) : 
                base(endpointConfigurationName) {
        }
        
        public Order_PlacementSoapClient(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public Order_PlacementSoapClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public Order_PlacementSoapClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress) {
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        AutoCarConsole.ACG_CK.HelloWorldResponse AutoCarConsole.ACG_CK.Order_PlacementSoap.HelloWorld(AutoCarConsole.ACG_CK.HelloWorldRequest request) {
            return base.Channel.HelloWorld(request);
        }
        
        public string HelloWorld(AutoCarConsole.ACG_CK.Auth_Header Auth_Header) {
            AutoCarConsole.ACG_CK.HelloWorldRequest inValue = new AutoCarConsole.ACG_CK.HelloWorldRequest();
            inValue.Auth_Header = Auth_Header;
            AutoCarConsole.ACG_CK.HelloWorldResponse retVal = ((AutoCarConsole.ACG_CK.Order_PlacementSoap)(this)).HelloWorld(inValue);
            return retVal.HelloWorldResult;
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        System.Threading.Tasks.Task<AutoCarConsole.ACG_CK.HelloWorldResponse> AutoCarConsole.ACG_CK.Order_PlacementSoap.HelloWorldAsync(AutoCarConsole.ACG_CK.HelloWorldRequest request) {
            return base.Channel.HelloWorldAsync(request);
        }
        
        public System.Threading.Tasks.Task<AutoCarConsole.ACG_CK.HelloWorldResponse> HelloWorldAsync(AutoCarConsole.ACG_CK.Auth_Header Auth_Header) {
            AutoCarConsole.ACG_CK.HelloWorldRequest inValue = new AutoCarConsole.ACG_CK.HelloWorldRequest();
            inValue.Auth_Header = Auth_Header;
            return ((AutoCarConsole.ACG_CK.Order_PlacementSoap)(this)).HelloWorldAsync(inValue);
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        AutoCarConsole.ACG_CK.URL_FileResponse AutoCarConsole.ACG_CK.Order_PlacementSoap.URL_File(AutoCarConsole.ACG_CK.URL_FileRequest request) {
            return base.Channel.URL_File(request);
        }
        
        public string[] URL_File(AutoCarConsole.ACG_CK.Auth_Header Auth_Header, AutoCarConsole.ACG_CK.File_Header File_Header, string URL) {
            AutoCarConsole.ACG_CK.URL_FileRequest inValue = new AutoCarConsole.ACG_CK.URL_FileRequest();
            inValue.Auth_Header = Auth_Header;
            inValue.File_Header = File_Header;
            inValue.URL = URL;
            AutoCarConsole.ACG_CK.URL_FileResponse retVal = ((AutoCarConsole.ACG_CK.Order_PlacementSoap)(this)).URL_File(inValue);
            return retVal.URL_FileResult;
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        System.Threading.Tasks.Task<AutoCarConsole.ACG_CK.URL_FileResponse> AutoCarConsole.ACG_CK.Order_PlacementSoap.URL_FileAsync(AutoCarConsole.ACG_CK.URL_FileRequest request) {
            return base.Channel.URL_FileAsync(request);
        }
        
        public System.Threading.Tasks.Task<AutoCarConsole.ACG_CK.URL_FileResponse> URL_FileAsync(AutoCarConsole.ACG_CK.Auth_Header Auth_Header, AutoCarConsole.ACG_CK.File_Header File_Header, string URL) {
            AutoCarConsole.ACG_CK.URL_FileRequest inValue = new AutoCarConsole.ACG_CK.URL_FileRequest();
            inValue.Auth_Header = Auth_Header;
            inValue.File_Header = File_Header;
            inValue.URL = URL;
            return ((AutoCarConsole.ACG_CK.Order_PlacementSoap)(this)).URL_FileAsync(inValue);
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        AutoCarConsole.ACG_CK.Place_OrdersResponse AutoCarConsole.ACG_CK.Order_PlacementSoap.Place_Orders(AutoCarConsole.ACG_CK.Place_OrdersRequest request) {
            return base.Channel.Place_Orders(request);
        }
        
        public AutoCarConsole.ACG_CK.Place_Order_Response Place_Orders(AutoCarConsole.ACG_CK.Auth_Header Auth_Header, AutoCarConsole.ACG_CK.Order[] Orders) {
            AutoCarConsole.ACG_CK.Place_OrdersRequest inValue = new AutoCarConsole.ACG_CK.Place_OrdersRequest();
            inValue.Auth_Header = Auth_Header;
            inValue.Orders = Orders;
            AutoCarConsole.ACG_CK.Place_OrdersResponse retVal = ((AutoCarConsole.ACG_CK.Order_PlacementSoap)(this)).Place_Orders(inValue);
            return retVal.Place_OrdersResult;
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        System.Threading.Tasks.Task<AutoCarConsole.ACG_CK.Place_OrdersResponse> AutoCarConsole.ACG_CK.Order_PlacementSoap.Place_OrdersAsync(AutoCarConsole.ACG_CK.Place_OrdersRequest request) {
            return base.Channel.Place_OrdersAsync(request);
        }
        
        public System.Threading.Tasks.Task<AutoCarConsole.ACG_CK.Place_OrdersResponse> Place_OrdersAsync(AutoCarConsole.ACG_CK.Auth_Header Auth_Header, AutoCarConsole.ACG_CK.Order[] Orders) {
            AutoCarConsole.ACG_CK.Place_OrdersRequest inValue = new AutoCarConsole.ACG_CK.Place_OrdersRequest();
            inValue.Auth_Header = Auth_Header;
            inValue.Orders = Orders;
            return ((AutoCarConsole.ACG_CK.Order_PlacementSoap)(this)).Place_OrdersAsync(inValue);
        }
    }
}
