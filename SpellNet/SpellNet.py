import cv2
import matplotlib.pyplot as plt
import numpy as np
import os
from tensorflow.keras.utils import to_categorical
from tensorflow.keras.models import Sequential
import tensorflow.keras.layers as layers
import keras2onnx
import onnx

import tensorflow as tf
# physical_devices = tf.config.list_physical_devices('GPU') 
# tf.config.experimental.set_memory_growth(physical_devices[0], True)


data_path = "D:\\Media\\WandShots\\"
kernel = np.ones((5,5))
size = (50,50)
test_proportion = 0.1
label_indicies = { # Must match with Spells enum in C# project

    "Dud": 0,
    "Lumos": 1,
    # "Balloonius Raisus": 1,
    "Shootify": 2,
    "Disneyosa": 3,
    "Wingardium Leviosa": 4,
    "Smallo Munchio": 5,
    "Funsizarth": 6,
    "Bigcandius": 7,
    "Obtainit": 8,
    "Reparo": 9,
}
label_names = {index: name for name, index in label_indicies.items()}

def load_data():
    data = []
    labels = []
    filenames = [filename for filename in os.listdir(data_path) if os.path.isfile(data_path+filename)]
    for filename in filenames:
        labels.append(filename.split("_")[0])
        im = cv2.imread(data_path+filename)
        # Preprocess
        im = cv2.dilate(im, kernel, iterations=2)
        im = cv2.resize(im, size)
        im = cv2.cvtColor(im, cv2.COLOR_BGR2GRAY)
        data.append(im)
    print("%s images loaded. Labels: %s" % (len(data), str.join(", ", set(labels))))
    data = np.array(data)
    global label_indicies
    labels = np.array([label_indicies[name] for name in labels])
    test_quantity = int(len(data)*test_proportion)
    indicies = np.random.choice(len(data), test_quantity, replace=False)
    test_data = data[indicies]
    test_labels = labels[indicies]
    mask = np.ones(len(data), bool)
    mask[indicies] = False
    train_data = data[mask]
    train_labels = labels[mask]
    return train_data, train_labels, test_data, test_labels

def preview(data):
    columns = int(np.floor(np.sqrt(len(data))))
    rows = int(np.floor(len(data)/columns))
    row_images = np.concatenate([data[i:i + columns] for i in range(0, columns*rows, columns)], axis=1)
    stacked_rows = np.concatenate(row_images, axis=1)
    plt.imshow(stacked_rows)
    plt.show()

def create_model(input_shape):
    model = Sequential([
        layers.Conv2D(32, kernel_size=(3, 3), activation="relu", input_shape=input_shape),
        layers.MaxPooling2D(pool_size=(2, 2)),
        layers.Dropout(0.15),
        layers.Conv2D(64, kernel_size=(3, 3), activation="relu"),
        layers.MaxPooling2D(pool_size=(2, 2)),
        layers.Flatten(),
        layers.Dropout(0.5),
        layers.Dense(len(label_names), activation="softmax"),
    ])
    model.compile(optimizer='rmsprop', loss='categorical_crossentropy', metrics=['accuracy'])
    return model




train_images, train_labels, test_images, test_labels = load_data()

# preview(train_images)
# cv2.waitKey()

dim_data = np.array((*train_images.shape[1:],1))
train_data = train_images.reshape(train_images.shape[0], *dim_data)
test_data = test_images.reshape(test_images.shape[0], *dim_data)
train_data = train_data.astype('float32')
test_data = test_data.astype('float32')
train_labels_one_hot = to_categorical(train_labels)
test_labels_one_hot = to_categorical(test_labels)

model = create_model((dim_data))

history = model.fit(train_data, train_labels_one_hot, batch_size=256, epochs=40, verbose=1,
    validation_data=(test_data, test_labels_one_hot))

[test_loss, test_acc] = model.evaluate(test_data, test_labels_one_hot)
print("Evaluation result on Test Data : Loss = {}, accuracy = {}".format(test_loss, test_acc))

#Plot the Loss Curves
plt.figure(figsize=[8,6])
plt.plot(history.history['loss'],'r',linewidth=3.0)
plt.plot(history.history['val_loss'],'b',linewidth=3.0)
plt.legend(['Training loss', 'Validation Loss'],fontsize=18)
plt.xlabel('Epochs ',fontsize=16)
plt.ylabel('Loss',fontsize=16)
plt.title('Loss Curves',fontsize=16)

#Plot the Accuracy Curves
plt.figure(figsize=[8,6]) 
plt.plot(history.history['accuracy'],'r',linewidth=3.0) 
plt.plot(history.history['val_accuracy'],'b',linewidth=3.0) 
plt.legend(['Training Accuracy', 'Validation Accuracy'],fontsize=18) 
plt.xlabel('Epochs ',fontsize=16) 
plt.ylabel('Accuracy',fontsize=16) 
plt.title('Accuracy Curves',fontsize=16)

plt.show()

model_onnx = keras2onnx.convert_keras(model)
onnx.save_model(model_onnx, "C:\\Source\\HarryPotterMagic\\SpellNet\\model.onnx")
print ("Onnx model saved")