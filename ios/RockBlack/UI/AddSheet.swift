import AVFoundation
import SwiftUI
import UIKit

/// Add a subscription URL, share links, Xray JSON — typed, pasted or scanned.
struct AddSheet: View {
    @EnvironmentObject var store: AppStore
    @Environment(\.dismiss) private var dismiss
    @State private var text = ""
    @State private var working = false
    @State private var scanning = false

    var body: some View {
        NavigationStack {
            Form {
                Section {
                    TextField(L("Subscription URL or share link"), text: $text, axis: .vertical)
                        .lineLimit(1...6)
                        .textInputAutocapitalization(.never)
                        .autocorrectionDisabled()
                        .keyboardType(.URL)
                    Button {
                        submit(text)
                    } label: {
                        HStack {
                            Text("Add")
                            if working { Spacer(); ProgressView() }
                        }
                    }
                    .disabled(text.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty || working)
                } footer: {
                    Text("Supported: subscription URLs, vless, vmess, trojan, ss, hysteria2, socks, wireguard links and Xray JSON.")
                }
                .listRowBackground(Palette.surface)

                Section {
                    Button {
                        if let clip = UIPasteboard.general.string, !clip.isEmpty {
                            submit(clip)
                        } else {
                            store.message = L("Clipboard is empty")
                        }
                    } label: {
                        Label("Add from clipboard", systemImage: "doc.on.clipboard")
                    }
                    Button { scanning = true } label: {
                        Label("Scan QR code", systemImage: "qrcode.viewfinder")
                    }
                }
                .listRowBackground(Palette.surface)
            }
            .scrollContentBackground(.hidden)
            .background(Palette.background)
            .navigationTitle(Text("Add"))
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) { Button("Cancel") { dismiss() } }
            }
            .sheet(isPresented: $scanning) {
                QRScannerSheet { code in
                    scanning = false
                    submit(code)
                }
            }
        }
    }

    private func submit(_ input: String) {
        working = true
        Task {
            let ok = await store.add(input)
            working = false
            if ok { dismiss() }
        }
    }
}

struct QRScannerSheet: View {
    var onCode: (String) -> Void
    @Environment(\.dismiss) private var dismiss
    @State private var denied = false

    var body: some View {
        NavigationStack {
            ZStack {
                Color.black.ignoresSafeArea()
                if denied {
                    VStack(spacing: 12) {
                        Text("Camera access is not available.").foregroundColor(.white)
                        Button("Open Settings") {
                            if let url = URL(string: UIApplication.openSettingsURLString) { UIApplication.shared.open(url) }
                        }
                    }
                } else {
                    QRScannerView(onCode: onCode, onFailure: { denied = true })
                        .ignoresSafeArea()
                    RoundedRectangle(cornerRadius: 24)
                        .stroke(Palette.primary, lineWidth: 3)
                        .frame(width: 240, height: 240)
                }
            }
            .navigationTitle(Text("Scan QR code"))
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) { Button("Cancel") { dismiss() } }
            }
        }
    }
}

struct QRScannerView: UIViewControllerRepresentable {
    var onCode: (String) -> Void
    var onFailure: () -> Void

    func makeUIViewController(context: Context) -> ScannerController {
        let controller = ScannerController()
        controller.onCode = onCode
        controller.onFailure = onFailure
        return controller
    }

    func updateUIViewController(_ controller: ScannerController, context: Context) {}

    final class ScannerController: UIViewController, AVCaptureMetadataOutputObjectsDelegate {
        var onCode: ((String) -> Void)?
        var onFailure: (() -> Void)?
        private let session = AVCaptureSession()
        private var preview: AVCaptureVideoPreviewLayer?
        private var delivered = false

        override func viewDidLoad() {
            super.viewDidLoad()
            view.backgroundColor = .black
            AVCaptureDevice.requestAccess(for: .video) { granted in
                DispatchQueue.main.async { granted ? self.configure() : self.onFailure?() }
            }
        }

        private func configure() {
            guard let device = AVCaptureDevice.default(for: .video),
                  let input = try? AVCaptureDeviceInput(device: device), session.canAddInput(input) else {
                onFailure?()
                return
            }
            session.addInput(input)
            let output = AVCaptureMetadataOutput()
            guard session.canAddOutput(output) else { onFailure?(); return }
            session.addOutput(output)
            output.setMetadataObjectsDelegate(self, queue: .main)
            output.metadataObjectTypes = [.qr]
            let layer = AVCaptureVideoPreviewLayer(session: session)
            layer.videoGravity = .resizeAspectFill
            layer.frame = view.bounds
            view.layer.addSublayer(layer)
            preview = layer
            DispatchQueue.global(qos: .userInitiated).async { self.session.startRunning() }
        }

        override func viewDidLayoutSubviews() {
            super.viewDidLayoutSubviews()
            preview?.frame = view.bounds
        }

        override func viewWillDisappear(_ animated: Bool) {
            super.viewWillDisappear(animated)
            if session.isRunning { session.stopRunning() }
        }

        func metadataOutput(_ output: AVCaptureMetadataOutput, didOutput metadataObjects: [AVMetadataObject],
                            from connection: AVCaptureConnection) {
            guard !delivered,
                  let code = (metadataObjects.first as? AVMetadataMachineReadableCodeObject)?.stringValue else { return }
            delivered = true
            session.stopRunning()
            UINotificationFeedbackGenerator().notificationOccurred(.success)
            onCode?(code)
        }
    }
}
