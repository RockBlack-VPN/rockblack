import CoreImage.CIFilterBuiltins
import SwiftUI

/// Server actions: ping, share / QR, copy link, edit, delete.
struct ServerDetailView: View {
    @EnvironmentObject var store: AppStore
    @Environment(\.dismiss) private var dismiss
    @State var server: Server
    @State private var editingJSON = false
    @State private var json = ""
    @State private var confirmDelete = false

    var body: some View {
        NavigationStack {
            Form {
                Section {
                    HStack(spacing: 12) {
                        FlagView(name: server.name)
                        VStack(alignment: .leading) {
                            Text(Flags.strippedName(server.name)).font(.headline)
                            Text(server.protocolName).font(.subheadline).foregroundColor(Palette.textMuted)
                        }
                        Spacer()
                        PingLabel(result: store.pings[server.id])
                    }
                    if let endpoint = server.endpoint {
                        LabeledContent(L("Address"), value: "\(endpoint.host):\(endpoint.port)")
                    }
                    TextField(L("Name"), text: $server.name)
                        .onSubmit { store.updateServer(server) }
                }
                .listRowBackground(Palette.surface)

                Section {
                    Button { store.ping([server]) } label: { Label("Ping", systemImage: "speedometer") }
                    Button { store.select(server); dismiss() } label: {
                        Label("Use this server", systemImage: "checkmark.circle")
                    }
                    if let link = store.shareLink(for: server) {
                        Button { store.copy(link) } label: { Label("Copy link", systemImage: "doc.on.doc") }
                        ShareLink(item: link) { Label("Share", systemImage: "square.and.arrow.up") }
                    }
                    Button {
                        json = JSONText.object(server.outbound).flatMap { JSONText.encode($0, pretty: true) } ?? server.outbound
                        editingJSON = true
                    } label: { Label("Edit configuration", systemImage: "curlybraces") }
                    Button(role: .destructive) { confirmDelete = true } label: { Label("Delete", systemImage: "trash") }
                }
                .listRowBackground(Palette.surface)

                if let link = store.shareLink(for: server), let image = QRCode.image(link) {
                    Section {
                        Image(uiImage: image)
                            .interpolation(.none)
                            .resizable()
                            .scaledToFit()
                            .frame(maxWidth: 240)
                            .padding(12)
                            .background(Color.white)
                            .clipShape(RoundedRectangle(cornerRadius: Metrics.controlRadius))
                            .frame(maxWidth: .infinity)
                    } header: {
                        Text("QR code")
                    }
                    .listRowBackground(Palette.surface)
                }
            }
            .scrollContentBackground(.hidden)
            .background(Palette.background)
            .navigationTitle(Text("Server"))
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .confirmationAction) {
                    Button("Done") { store.updateServer(server); dismiss() }
                }
            }
            .confirmationDialog(Text("Delete this server?"), isPresented: $confirmDelete, titleVisibility: .visible) {
                Button("Delete", role: .destructive) { store.deleteServer(server.id); dismiss() }
            }
            .sheet(isPresented: $editingJSON) {
                NavigationStack {
                    TextEditor(text: $json)
                        .font(.system(.footnote, design: .monospaced))
                        .autocorrectionDisabled()
                        .textInputAutocapitalization(.never)
                        .padding(8)
                        .navigationTitle(Text("Xray outbound"))
                        .navigationBarTitleDisplayMode(.inline)
                        .toolbar {
                            ToolbarItem(placement: .cancellationAction) { Button("Cancel") { editingJSON = false } }
                            ToolbarItem(placement: .confirmationAction) {
                                Button("Save") {
                                    if let object = JSONText.object(json), let compact = JSONText.encode(object) {
                                        server.outbound = compact
                                        server.link = nil
                                        store.updateServer(server)
                                        editingJSON = false
                                    } else {
                                        store.message = L("Invalid JSON")
                                    }
                                }
                            }
                        }
                }
            }
        }
    }
}

enum QRCode {
    static func image(_ text: String) -> UIImage? {
        let filter = CIFilter.qrCodeGenerator()
        filter.message = Data(text.utf8)
        filter.correctionLevel = "M"
        guard let output = filter.outputImage?.transformed(by: CGAffineTransform(scaleX: 8, y: 8)),
              let cg = CIContext().createCGImage(output, from: output.extent) else { return nil }
        return UIImage(cgImage: cg)
    }
}
