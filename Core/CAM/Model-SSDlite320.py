        import torch
import torch.nn as nn
import math
import sys
import os
from spikingjelly.clock_driven import layer
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from Neurons.Neuron import LMHT_LIF, STEGradientFunction, PiecewiseLinearGradientFunction


def conv_bn(inp, oup, stride, Time_step=8, Num_thresholds=16):
    return nn.Sequential(
        layer.SeqToANNContainer(
        nn.Conv2d(inp, oup, 3, stride, 1, bias=False),
        nn.BatchNorm2d(oup),
        ),
        LMHT_LIF(threshold=1.0, Time_step=Time_step, leakage=0., 
                           Num_thresholds=Num_thresholds, Reset_mode='soft', 
                           V_init=0., alpha=1.0, Activation='STEGradientFunction')
    )


def conv_1x1_bn(inp, oup, Time_step=8, Num_thresholds=16):
    return nn.Sequential(
        layer.SeqToANNContainer(
        nn.Conv2d(inp, oup, 1, 1, 0, bias=False),
        nn.BatchNorm2d(oup)
        ),
        LMHT_LIF(threshold=1.0, Time_step=Time_step, leakage=0., 
                           Num_thresholds=Num_thresholds, Reset_mode='soft', 
                           V_init=0., alpha=1.0, Activation='STEGradientFunction')
    )


def make_divisible(x, divisible_by=8):
    import numpy as np
    return int(np.ceil(x * 1. / divisible_by) * divisible_by)


class InvertedResidual(nn.Module):
    def __init__(self, inp, oup, stride, expand_ratio, Time_step, Num_thresholds):
        super(InvertedResidual, self).__init__()
        self.stride = stride
        self.Time_step = Time_step
        self.Num_thresholds = Num_thresholds
        assert stride in [1, 2]

        hidden_dim = int(inp * expand_ratio)
        self.use_res_connect = self.stride == 1 and inp == oup

        if expand_ratio == 1:
            self.conv = nn.Sequential(
                # dw
                layer.SeqToANNContainer(
                nn.Conv2d(hidden_dim, hidden_dim, 3, stride, 1, groups=hidden_dim, bias=False),
                nn.BatchNorm2d(hidden_dim)
                ),
                LMHT_LIF(threshold=1.0, Time_step=Time_step, leakage=0., 
                           Num_thresholds=Num_thresholds, Reset_mode='soft', 
                           V_init=0., alpha=1.0, Activation='STEGradientFunction'),
                # pw-linear
                layer.SeqToANNContainer(
                nn.Conv2d(hidden_dim, oup, 1, 1, 0, bias=False),
                nn.BatchNorm2d(oup)),
            )
        else:
            self.conv = nn.Sequential(
                # pw
                layer.SeqToANNContainer(
                nn.Conv2d(inp, hidden_dim, 1, 1, 0, bias=False),
                nn.BatchNorm2d(hidden_dim)
                ),
                LMHT_LIF(threshold=1.0, Time_step=Time_step, leakage=0., 
                           Num_thresholds=Num_thresholds, Reset_mode='soft', 
                           V_init=0., alpha=1.0, Activation='STEGradientFunction'),
                # dw
                layer.SeqToANNContainer(
                nn.Conv2d(hidden_dim, hidden_dim, 3, stride, 1, groups=hidden_dim, bias=False),
                nn.BatchNorm2d(hidden_dim)
                ),
                LMHT_LIF(threshold=1.0, Time_step=Time_step, leakage=0., 
                           Num_thresholds=Num_thresholds, Reset_mode='soft', 
                           V_init=0., alpha=1.0, Activation='STEGradientFunction'),
                # pw-linear
                layer.SeqToANNContainer(
                nn.Conv2d(hidden_dim, oup, 1, 1, 0, bias=False),
                nn.BatchNorm2d(oup)
                ),
            )

    def forward(self, x):
        if self.use_res_connect:
            return x + self.conv(x)
        else:
            return self.conv(x)


class MobileNetV2(nn.Module):
    def __init__(self, n_class=1000, input_size=224, width_mult=1., Time_step=4, Num_thresholds=2):
        super(MobileNetV2, self).__init__()
        self.Time_step = Time_step
        self.Num_thresholds = Num_thresholds
        block = InvertedResidual
        input_channel = 32
        last_channel = 128
        interverted_residual_setting = [
            # t, c, n, s
            [6, 32, 1, 1],    
            [6, 64, 1, 1],   
            [6, 128, 1, 1],   
            [6, 128, 1, 1]   
        ]

        # building first layer
        assert input_size % 32 == 0
        self.last_channel = make_divisible(last_channel * width_mult) if width_mult > 1.0 else last_channel
        self.features = [conv_bn(3, input_channel, 1, Time_step=Time_step, Num_thresholds=Num_thresholds)]
        self.maxpool2x2 = layer.SeqToANNContainer(
            nn.MaxPool2d(kernel_size=2, stride=2)
        )
        # building inverted residual blocks
        for t, c, n, s in interverted_residual_setting:
            output_channel = make_divisible(c * width_mult) if t > 1 else c
            for i in range(n):
                if i == 0:
                    self.features.append(block(input_channel, output_channel, s, expand_ratio=t, Time_step=Time_step, Num_thresholds=Num_thresholds))
                else:
                    self.features.append(block(input_channel, output_channel, 1, expand_ratio=t, Time_step=Time_step, Num_thresholds=Num_thresholds))
                input_channel = output_channel
            self.features.append(self.maxpool2x2)
                
        # building last several layers
        self.features.append(conv_1x1_bn(input_channel, self.last_channel, Time_step=Time_step, Num_thresholds=Num_thresholds))
        # make it nn.Sequential
        self.features = nn.Sequential(*self.features)

        # building classifier
        self.classifier = nn.Linear(self.last_channel, n_class)
        self._initialize_weights()

    def forward(self, x):

        x = self.features(x)  # Output: (T, B, C, H, W)

        x = x.sum(0)
        x = nn.functional.adaptive_max_pool2d(x, (1, 1))
        x = x.squeeze(-1).squeeze(-1)  # (B, C)
        x = self.classifier(x)  # (B, n_class)


        return x # (B, n_class) 

    def _initialize_weights(self):
        for m in self.modules():
            if isinstance(m, nn.Conv2d):
                n = m.kernel_size[0] * m.kernel_size[1] * m.out_channels
                m.weight.data.normal_(0, math.sqrt(2. / n))
                if m.bias is not None:
                    m.bias.data.zero_()
            elif isinstance(m, nn.BatchNorm2d):
                m.weight.data.fill_(1)
                m.bias.data.zero_()
            elif isinstance(m, nn.Linear):
                n = m.weight.size(1)
                m.weight.data.normal_(0, 0.01)
                m.bias.data.zero_()

def mobilenet_v2(width_mult=1., Time_step=4, Num_thresholds=2):
    model = MobileNetV2(width_mult=width_mult, Time_step=Time_step, Num_thresholds=Num_thresholds)
    return model
class SSDLiteHead(nn.Module):
    def __init__(self, in_channels, num_classes):
        super(SSDLiteHead, self).__init__()
        self.num_classes = num_classes
        self.loc_layers = nn.ModuleList()
        self.cls_layers = nn.ModuleList()
        # mỗi feature map sẽ có một conv để dự đoán
        for c in in_channels:
            self.loc_layers.append(
                nn.Conv2d(c, 6 * 4, kernel_size=3, padding=1)  # 6 anchors * 4 toạ độ
            )
            self.cls_layers.append(
                nn.Conv2d(c, 6 * num_classes, kernel_size=3, padding=1)
            )

    def forward(self, features):
        locs = []
        confs = []
        for x, l, c in zip(features, self.loc_layers, self.cls_layers):
            locs.append(l(x).permute(0, 2, 3, 1).contiguous())
            confs.append(c(x).permute(0, 2, 3, 1).contiguous())
        return locs, confs
class Spiking_SSDLite320(nn.Module):
    def __init__(self, num_classes=20, width_mult=1.0, Time_step=4, Num_thresholds=2):
        super(Spiking_SSDLite320, self).__init__()
        # backbone
        self.backbone = MobileNetV2(
            n_class=num_classes, 
            input_size=320, 
            width_mult=width_mult,
            Time_step=Time_step, 
            Num_thresholds=Num_thresholds
        ).features

        # các feature map cần lấy ra
        self.feature_indices = [3, 6, 10, 13]  # chọn các tầng khác nhau

        # extra feature layers (giống SSD)
        self.extra = nn.ModuleList([
            conv_1x1_bn(128, 256, Time_step, Num_thresholds),
            conv_1x1_bn(256, 512, Time_step, Num_thresholds),
        ])

        # head SSDlite
        self.head = SSDLiteHead([32, 64, 128, 256, 512], num_classes)

    def forward(self, x):
        features = []
        for i, layer in enumerate(self.backbone):
            x = layer(x)
            if i in self.feature_indices:
                features.append(x)
        for l in self.extra:
            x = l(x)
            features.append(x)
        locs, confs = self.head(features)
        return locs, confs



if __name__ == '__main__':
    net = mobilenet_v2(width_mult=0.5, Time_step=4, Num_thresholds=4)
    Time_step = 4
    model_size = sum(p.numel() for p in net.parameters() if p.requires_grad)
    print("Model size (parameters):", model_size)
    # shape (T, B, C, H, W)
    input = torch.randn(Time_step, 8, 3, 32, 32)
    print("Input shape:", input.shape)
    output = net(input)
    print("Output shape:", output.shape)
    print("Output:", output)
    model = Spiking_SSDLite320(num_classes=5, width_mult=0.5)
    input = torch.randn(4, 8, 3, 320, 320)  # (T, B, C, H, W)
    with torch.no_grad():
        locs, confs = model(input)
    for i, (l, c) in enumerate(zip(locs, confs)):
        print(f"Feature {i}: loc={l.shape}, conf={c.shape}")


